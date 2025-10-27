using SummerCampManagementSystem.BLL.DTOs.Transaction;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface ITransactionService
    {
        Task<IEnumerable<TransactionResponseDto>> GetUserTransactionHistoryAsync();
        Task<IEnumerable<TransactionResponseDto>> GetTransactionsByRegistrationIdAsync(int registrationId);
        Task<IEnumerable<TransactionResponseDto>> GetAllTransactionsAsync();
        Task<TransactionResponseDto?> GetTransactionByIdAsync(int id);
        Task<IEnumerable<TransactionResponseDto>> GetTransactionsByCampIdAsync(int campId);
    }
}
