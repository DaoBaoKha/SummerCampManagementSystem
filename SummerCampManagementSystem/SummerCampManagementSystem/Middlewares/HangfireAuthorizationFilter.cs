using Hangfire.Dashboard;
using Microsoft.Extensions.Logging;

namespace SummerCampManagementSystem.API.Middlewares
{
    /// <summary>
    /// Authorization filter for Hangfire Dashboard
    /// Allows access to authenticated admin users
    /// </summary>
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        private readonly ILogger<HangfireAuthorizationFilter> _logger;
        private readonly IConfiguration _configuration;

        public HangfireAuthorizationFilter(ILogger<HangfireAuthorizationFilter> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();

            // Check if authentication is required from configuration
            var requireAuth = _configuration.GetValue<bool>("Hangfire:RequireAuthentication", true);

            if (!requireAuth)
            {
                _logger.LogWarning("Hangfire dashboard authentication is DISABLED - allowing unrestricted access");
                return true;
            }

            // Allow local requests in development
            if (httpContext.Request.Host.Host == "localhost" ||
                httpContext.Request.Host.Host == "127.0.0.1" ||
                httpContext.Connection.RemoteIpAddress?.ToString() == "::1")
            {
                _logger.LogDebug("Hangfire dashboard access granted for local request");
                return true;
            }

            // Check if user is authenticated
            if (!httpContext.User.Identity?.IsAuthenticated ?? true)
            {
                _logger.LogWarning("Hangfire dashboard access denied: User not authenticated");
                return false;
            }

            // Check if user has required role
            var allowedRoles = _configuration.GetSection("Hangfire:AllowedRoles").Get<string[]>()
                ?? new[] { "Admin" };

            foreach (var role in allowedRoles)
            {
                if (httpContext.User.IsInRole(role))
                {
                    _logger.LogInformation("Hangfire dashboard access granted for user {User} with role {Role}",
                        httpContext.User.Identity?.Name ?? "Unknown", role);
                    return true;
                }
            }

            _logger.LogWarning("Hangfire dashboard access denied: User {User} does not have required role. Required roles: {Roles}",
                httpContext.User.Identity?.Name ?? "Unknown", string.Join(", ", allowedRoles));
            return false;
        }
    }
}
