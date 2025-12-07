using SummerCampManagementSystem.BLL.DTOs.Refund;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface IRefundService
    {        
        Task<RefundCalculationDto> CalculateRefundAsync(int registrationId);

        Task<RegistrationCancelResponseDto> RequestCancelAsync(CancelRequestDto requestDto);

        Task<IEnumerable<RefundRequestListDto>> GetAllRefundRequestsAsync(RefundRequestFilterDto? filter = null);

        Task<RegistrationCancelResponseDto> ApproveRefundAsync(ApproveRefundDto dto);

        Task<RegistrationCancelResponseDto> RejectRefundAsync(RejectRefundDto dto);
    }
}
