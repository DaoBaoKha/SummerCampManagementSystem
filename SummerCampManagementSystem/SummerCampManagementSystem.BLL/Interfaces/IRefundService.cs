using SummerCampManagementSystem.BLL.DTOs.Refund;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface IRefundService
    {        
        Task<RefundCalculationDto> CalculateRefundAsync(int registrationId);

        Task<RegistrationCancelResponseDto> RequestCancelAsync(CancelRequestDto requestDto);
    }
}
