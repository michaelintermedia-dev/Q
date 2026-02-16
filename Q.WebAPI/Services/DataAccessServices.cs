using Microsoft.EntityFrameworkCore;
using Q.WebAPI.Models.DbData;

namespace Q.WebAPI.Services
{
    public interface IDataAccessService
    {
        Task<Q.WebAPI.Models.DbData.Appointment> SaveAppointment(Appointment appointment);
        Task<List<Models.DbData.Appointment>> GetAppointmentsByUserId(int userId);




        #region Auth

        Task<bool> UserExistsByEmailAsync(string email);
        Task<User?> GetUserByEmailAsync(string email);
        Task<User?> GetUserByVerificationTokenAsync(string token);
        Task<User?> GetUserByPasswordResetTokenAsync(string token);
        Task<User> CreateUserAsync(User user);
        Task UpdateUserAsync(User user);
        Task<UserSession?> GetActiveSessionByRefreshTokenAsync(string refreshToken);
        Task<UserSession?> GetSessionByUserAndTokenAsync(int userId, string refreshToken);
        Task CreateSessionAsync(UserSession session);
        Task UpdateSessionAsync(UserSession session);
        Task<UserDevice?> GetUserDeviceAsync(int userId, string deviceToken);
        Task CreateUserDeviceAsync(UserDevice device);
        Task UpdateUserDeviceAsync(UserDevice device);
        Task SaveChangesAsync(); 
        #endregion
    }

    public class DataAccessService : IDataAccessService
    {
        private readonly QContext _context;

        public DataAccessService(QContext context)
        {
            _context = context;
        }

        #region Auth
        public async Task<bool> UserExistsByEmailAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetUserByVerificationTokenAsync(string token)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.EmailVerificationToken == token);
        }

        public async Task<User?> GetUserByPasswordResetTokenAsync(string token)
        {
            return await _context.Users.FirstOrDefaultAsync(u =>
                u.PasswordResetToken == token && u.PasswordResetTokenExpiry > DateTime.UtcNow);
        }

        public async Task<User> CreateUserAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task UpdateUserAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        public async Task<UserSession?> GetActiveSessionByRefreshTokenAsync(string refreshToken)
        {
            return await _context.UserSessions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.RefreshToken == refreshToken && s.RevokedAt == null);
        }

        public async Task<UserSession?> GetSessionByUserAndTokenAsync(int userId, string refreshToken)
        {
            return await _context.UserSessions
                .FirstOrDefaultAsync(s => s.UserId == userId && s.RefreshToken == refreshToken);
        }

        public async Task CreateSessionAsync(UserSession session)
        {
            _context.UserSessions.Add(session);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateSessionAsync(UserSession session)
        {
            _context.UserSessions.Update(session);
            await _context.SaveChangesAsync();
        }

        public async Task<UserDevice?> GetUserDeviceAsync(int userId, string deviceToken)
        {
            return await _context.UserDevices
                .FirstOrDefaultAsync(d => d.UserId == userId && d.DeviceToken == deviceToken);
        }

        public async Task CreateUserDeviceAsync(UserDevice device)
        {
            _context.UserDevices.Add(device);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateUserDeviceAsync(UserDevice device)
        {
            _context.UserDevices.Update(device);
            await _context.SaveChangesAsync();
        } 
        #endregion

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<Appointment> SaveAppointment(Appointment appointment)
        {
            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            return appointment;
        }

        public async Task<List<Models.DbData.Appointment>> GetAppointmentsByUserId(int userId)
        {
            return await _context.Appointments.Where(a => a.UserId == userId).ToListAsync();
        }
    }
}
