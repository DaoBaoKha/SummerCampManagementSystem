using SummerCampManagementSystem.BLL.DTOs.Camp;
using SummerCampManagementSystem.Core.Enums;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface ICampService
    {
        Task<IEnumerable<CampResponseDto>> GetAllCampsAsync();
        Task<CampResponseDto?> GetCampByIdAsync(int id);
        Task<IEnumerable<CampResponseDto>> GetCampsByTypeAsync(int campTypeId);
        Task<IEnumerable<CampResponseDto>> GetCampsByStatusAsync(CampStatus? status = null);
        Task<CampResponseDto> CreateCampAsync(CampRequestDto camp);
        Task<CampResponseDto> UpdateCampAsync(int campId, CampRequestDto camp);
        Task<CampResponseDto> UpdateCampStatusAsync(int campId, CampStatusUpdateRequestDto statusUpdate);
        Task<bool> DeleteCampAsync(int id);
        Task<CampResponseDto> TransitionCampStatusAsync(int campId, CampStatus newStatus);
        Task<CampResponseDto> SubmitForApprovalAsync(int campId);
        Task<CampResponseDto> RejectCampAsync(int campId, CampRejectRequestDto request);
        Task<CampResponseDto> CancelCampAsync(int campId, CampCancelRequestDto request);
        Task<IEnumerable<CampResponseDto>> GetCampsByStaffIdAsync(int staffId);

        // workflow automation
        Task RunScheduledStatusTransitionsAsync();

        Task<CampResponseDto> ExtendRegistrationAsync(int campId, DateTime newRegistrationEndDate);
        Task<CampResponseDto> UpdateCampStatusNoValidationAsync(int campId, CampStatus newStatus);
        Task<CampValidationResponseDto> ValidateCampReadinessAsync(int campId);
    }
}
