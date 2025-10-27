using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.BLL.DTOs.Transaction;
using SummerCampManagementSystem.BLL.Helpers;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;

namespace SummerCampManagementSystem.BLL.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper; 
        private readonly IUserContextService _userContextService;

        public TransactionService(IUnitOfWork unitOfWork, IMapper mapper, IUserContextService userContextService) 
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper; 
            _userContextService = userContextService;
        }

        private IQueryable<Transaction> GetTransactionsWithIncludes()
        {
            return _unitOfWork.Transactions.GetQueryable()
                .Include(t => t.registration)
                .Include(t => t.registration.camp)
                .OrderByDescending(t => t.transactionTime);
        }


        public async Task<IEnumerable<TransactionResponseDto>> GetUserTransactionHistoryAsync()
        {
            var currentUserId = _userContextService.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                throw new UnauthorizedAccessException("User ID is missing from the token. Please log in again.");
            }

            var transactions = await GetTransactionsWithIncludes()
                .Where(t => t.registration != null && t.registration.userId == currentUserId.Value)
                .ToListAsync();

            return _mapper.Map<IEnumerable<TransactionResponseDto>>(transactions);
        }

        public async Task<IEnumerable<TransactionResponseDto>> GetTransactionsByRegistrationIdAsync(int registrationId)
        {
            var currentUserId = _userContextService.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                throw new UnauthorizedAccessException("User ID is missing from the token. Please log in again.");
            }

            var registration = await _unitOfWork.Registrations.GetByIdAsync(registrationId);
            if (registration == null)
            {
                throw new KeyNotFoundException($"Registration with ID {registrationId} not found.");
            }
            if (registration.userId != currentUserId.Value)
            {
                throw new UnauthorizedAccessException("You do not have permission to view transactions for this registration.");
            }

            var transactions = await GetTransactionsWithIncludes()
                .Where(t => t.registrationId == registrationId)
                .ToListAsync();

            return _mapper.Map<IEnumerable<TransactionResponseDto>>(transactions);
        }

        // admin
        public async Task<IEnumerable<TransactionResponseDto>> GetAllTransactionsAsync()
        {
            var transactions = await GetTransactionsWithIncludes()
                .ToListAsync();

            return _mapper.Map<IEnumerable<TransactionResponseDto>>(transactions);
        }

        public async Task<TransactionResponseDto?> GetTransactionByIdAsync(int id)
        {
            var transaction = await GetTransactionsWithIncludes()
                .FirstOrDefaultAsync(t => t.transactionId == id);

            return transaction == null ? null : _mapper.Map<TransactionResponseDto>(transaction);
        }

        public async Task<IEnumerable<TransactionResponseDto>> GetTransactionsByCampIdAsync(int campId)
        {
            var campExists = await _unitOfWork.Camps.GetByIdAsync(campId);
            if (campExists == null)
            {
                throw new KeyNotFoundException($"Camp with ID {campId} not found.");
            }

            var transactions = await GetTransactionsWithIncludes()
                .Where(t => t.registration != null && t.registration.campId == campId)
                .ToListAsync();

            return _mapper.Map<IEnumerable<TransactionResponseDto>>(transactions);
        }
    }
}
