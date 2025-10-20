using System.Security.Claims;

namespace SummerCampManagementSystem.BLL.Helpers
{
    public interface IUserContextService
    {
        int? GetCurrentUserId();
        ClaimsPrincipal? GetCurrentUser();
    }
}
