using SummerCampManagementSystem.BLL.DTOs.BankUser;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface IBankUserService
    {
        Task<IEnumerable<BankUserResponseDto>> GetMyBankAccountsAsync();
        Task<BankUserResponseDto> AddBankAccountAsync(BankUserRequestDto requestDto);
        Task<BankUserResponseDto> UpdateBankAccountAsync(int bankUserId, BankUserRequestDto requestDto);
        Task<bool> DeleteBankAccountAsync(int bankUserId);
        Task<bool> SetPrimaryBankAccountAsync(int bankUserId);
    }
}
