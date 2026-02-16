using System.Security.Claims;

namespace Q.WebAPI.Services
{
    public class UserService
    {
        public int UserId { get; init; }
        public UserService(IHttpContextAccessor httpContextAccessor)
        {
            var userIdClaim = httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var userId))
            {
                UserId = userId;
            }
        }
    }
}
