using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Q.WebAPI.Models.DbData;

namespace Q.WebAPI.Services;

public interface IAuthService
{
    Task<(bool success, string? token, string? refreshToken, string? message)> RegisterAsync(
        string email, string password, string? firstName, string? lastName);

    Task<(bool success, string? token, string? refreshToken, string? message, int? userId)> LoginAsync(
        string email, string password, string? deviceToken, string? platform);

    Task<(bool success, string? token, string? refreshToken, string? message)> RefreshTokenAsync(
        string refreshToken);

    Task<bool> LogoutAsync(int userId, string refreshToken);

    Task<bool> VerifyEmailAsync(string token);

    Task<bool> RequestPasswordResetAsync(string email);

    Task<bool> ResetPasswordAsync(string token, string newPassword);
}
public class AuthService : IAuthService
{
    private readonly IDataAccessService _dataAccess;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IDataAccessService dataAccess,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _dataAccess = dataAccess;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<(bool success, string? token, string? refreshToken, string? message)> RegisterAsync(
        string email, string password, string? firstName, string? lastName)
    {
        try
        {
            // Check if user exists
            if (await _dataAccess.UserExistsByEmailAsync(email))
            {
                return (false, null, null, "User with this email already exists");
            }

            // Create password hash
            var (hash, salt) = HashPassword(password);

            // Create user
            var user = new User
            {
                Email = email,
                PasswordHash = hash,
                PasswordSalt = salt,
                FirstName = firstName,
                LastName = lastName,
                EmailVerificationToken = GenerateToken(),
                IsEmailVerified = false
            };

            await _dataAccess.CreateUserAsync(user);

            _logger.LogInformation("User registered: {Email}", email);

            // Generate JWT token
            var token = GenerateJwtToken(user);
            var refreshToken = await CreateRefreshTokenAsync(user.Id);

            return (true, token, refreshToken, "Registration successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for {Email}", email);
            return (false, null, null, "Registration failed");
        }
    }

    public async Task<(bool success, string? token, string? refreshToken, string? message, int? userId)> LoginAsync(
        string email, string password, string? deviceToken, string? platform)
    {
        try
        {
            var user = await _dataAccess.GetUserByEmailAsync(email);

            if (user == null)
            {
                return (false, null, null, "Invalid email or password", null);
            }

            // Verify password
            if (!VerifyPassword(password, user.PasswordHash, user.PasswordSalt))
            {
                return (false, null, null, "Invalid email or password", null);
            }

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await _dataAccess.UpdateUserAsync(user);

            // Register device if provided
            if (!string.IsNullOrEmpty(deviceToken) && !string.IsNullOrEmpty(platform))
            {
                await RegisterDeviceAsync(user.Id, deviceToken, platform);
            }

            // Generate tokens
            var token = GenerateJwtToken(user);
            var refreshToken = await CreateRefreshTokenAsync(user.Id);

            _logger.LogInformation("User logged in: {Email}", email);

            return (true, token, refreshToken, "Login successful", user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for {Email}", email);
            return (false, null, null, "Login failed", null);
        }
    }

    public async Task<(bool success, string? token, string? refreshToken, string? message)> RefreshTokenAsync(
        string refreshToken)
    {
        var session = await _dataAccess.GetActiveSessionByRefreshTokenAsync(refreshToken);

        if (session == null || session.ExpiresAt < DateTime.UtcNow)
        {
            return (false, null, null, "Invalid or expired refresh token");
        }

        // Revoke old token
        session.RevokedAt = DateTime.UtcNow;
        await _dataAccess.UpdateSessionAsync(session);

        // Generate new tokens
        var newToken = GenerateJwtToken(session.User);
        var newRefreshToken = await CreateRefreshTokenAsync(session.UserId);

        return (true, newToken, newRefreshToken, "Token refreshed");
    }

    public async Task<bool> LogoutAsync(int userId, string refreshToken)
    {
        var session = await _dataAccess.GetSessionByUserAndTokenAsync(userId, refreshToken);

        if (session != null)
        {
            session.RevokedAt = DateTime.UtcNow;
            await _dataAccess.UpdateSessionAsync(session);
        }

        return true;
    }

    public async Task<bool> VerifyEmailAsync(string token)
    {
        var user = await _dataAccess.GetUserByVerificationTokenAsync(token);

        if (user == null)
            return false;

        user.IsEmailVerified = true;
        user.EmailVerificationToken = null;
        await _dataAccess.UpdateUserAsync(user);

        return true;
    }

    public async Task<bool> RequestPasswordResetAsync(string email)
    {
        var user = await _dataAccess.GetUserByEmailAsync(email);

        if (user == null)
            return false;

        user.PasswordResetToken = GenerateToken();
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);
        await _dataAccess.UpdateUserAsync(user);

        // TODO: Send email with reset link
        _logger.LogInformation("Password reset requested for {Email}", email);

        return true;
    }

    public async Task<bool> ResetPasswordAsync(string token, string newPassword)
    {
        var user = await _dataAccess.GetUserByPasswordResetTokenAsync(token);

        if (user == null)
            return false;

        var (hash, salt) = HashPassword(newPassword);
        user.PasswordHash = hash;
        user.PasswordSalt = salt;
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;

        await _dataAccess.UpdateUserAsync(user);

        return true;
    }

    private async Task RegisterDeviceAsync(int userId, string deviceToken, string platform)
    {
        var existingDevice = await _dataAccess.GetUserDeviceAsync(userId, deviceToken);

        if (existingDevice != null)
        {
            existingDevice.LastActiveAt = DateTime.UtcNow;
            await _dataAccess.UpdateUserDeviceAsync(existingDevice);
        }
        else
        {
            await _dataAccess.CreateUserDeviceAsync(new UserDevice
            {
                UserId = userId,
                DeviceToken = deviceToken,
                Platform = platform
            });
        }
    }

    private string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT key not configured")));
        
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("isEmailVerified", user.IsEmailVerified.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<string> CreateRefreshTokenAsync(int userId)
    {
        var refreshToken = GenerateToken();

        await _dataAccess.CreateSessionAsync(new UserSession
        {
            UserId = userId,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        });

        return refreshToken;
    }

    private (string hash, string salt) HashPassword(string password)
    {
        using var hmac = new HMACSHA512();
        var salt = Convert.ToBase64String(hmac.Key);
        var hash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(password)));
        return (hash, salt);
    }

    private bool VerifyPassword(string password, string storedHash, string storedSalt)
    {
        var saltBytes = Convert.FromBase64String(storedSalt);
        using var hmac = new HMACSHA512(saltBytes);
        var computedHash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(password)));
        return computedHash == storedHash;
    }

    private string GenerateToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }
}