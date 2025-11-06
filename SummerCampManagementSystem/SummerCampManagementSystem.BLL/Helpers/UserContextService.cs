using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace SummerCampManagementSystem.BLL.Helpers
{
    public class UserContextService : IUserContextService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserContextService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public ClaimsPrincipal? GetCurrentUser()
        {
            return _httpContextAccessor.HttpContext?.User;
        }

        public int? GetCurrentUserId()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return null;

            // find using multiple possible claim types for user ID
            var userIdClaim = httpContext.User.FindFirst("id");

            if (userIdClaim == null)
            {
                userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            }

            if (userIdClaim == null)
            {
                userIdClaim = httpContext.User.FindFirst(JwtRegisteredClaimNames.Sub);
            }

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return null;
            }

            return userId;
        }
    }
}
