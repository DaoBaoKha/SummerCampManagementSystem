using SummerCampManagementSystem.Core.Enums;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface ICampStatusService
    {
        Task<bool> TransitionCampStatusSafeAsync(int campId, CampStatus newStatus, string executionSource);
    }
}
