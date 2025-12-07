using AutoMapper;
using SummerCampManagementSystem.BLL.DTOs.BankUser;
using SummerCampManagementSystem.BLL.Exceptions;
using SummerCampManagementSystem.BLL.Helpers;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;

namespace SummerCampManagementSystem.BLL.Services
{
    public class BankUserService : IBankUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IUserContextService _userContextService;

        public BankUserService(IUnitOfWork unitOfWork, IMapper mapper, IUserContextService userContextService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _userContextService = userContextService;
        }

        public async Task<IEnumerable<BankUserResponseDto>> GetMyBankAccountsAsync()
        {
            var userId = GetCurrentUserId();

            var bankAccounts = await _unitOfWork.BankUsers.GetByUserIdAsync(userId); //already get status = true

            return _mapper.Map<IEnumerable<BankUserResponseDto>>(bankAccounts);
        }

        public async Task<BankUserResponseDto> AddBankAccountAsync(BankUserRequestDto requestDto)
        {
            var userId = GetCurrentUserId();

            // validate duplicate info
            var existingAccounts = await _unitOfWork.BankUsers.GetByUserIdAsync(userId);
            if (existingAccounts.Any(b => b.bankNumber == requestDto.BankNumber && b.bankCode == requestDto.BankCode))
            {
                throw new BadRequestException("Tài khoản ngân hàng này đã tồn tại trong danh sách của bạn.");
            }

            var newBankAccount = _mapper.Map<BankUser>(requestDto);
            newBankAccount.userId = userId;
            newBankAccount.isActive = true;

            // auto set primary logic
            if (!existingAccounts.Any())
            {
                newBankAccount.isPrimary = true;
            }
            else if (requestDto.IsPrimary)
            {
                await ResetPrimaryStatus(userId);
                newBankAccount.isPrimary = true;
            }
            else
            {
                newBankAccount.isPrimary = false;
            }

            await _unitOfWork.BankUsers.CreateAsync(newBankAccount);
            await _unitOfWork.CommitAsync();

            return _mapper.Map<BankUserResponseDto>(newBankAccount);
        }

        public async Task<BankUserResponseDto> UpdateBankAccountAsync(int bankUserId, BankUserRequestDto requestDto)
        {
            var userId = GetCurrentUserId();
            var bankAccount = await _unitOfWork.BankUsers.GetByIdAsync(bankUserId);

            // check exist and ownership
            if (bankAccount == null || bankAccount.userId != userId || bankAccount.isActive != true)
            {
                throw new NotFoundException("Không tìm thấy tài khoản ngân hàng hoặc bạn không có quyền chỉnh sửa.");
            }

            if (requestDto.IsPrimary && bankAccount.isPrimary != true)
            {
                await ResetPrimaryStatus(userId);
            }

            _mapper.Map(requestDto, bankAccount);

            bankAccount.isActive = true;

            await _unitOfWork.BankUsers.UpdateAsync(bankAccount);
            await _unitOfWork.CommitAsync();

            return _mapper.Map<BankUserResponseDto>(bankAccount);
        }

        public async Task<bool> DeleteBankAccountAsync(int bankUserId)
        {
            var userId = GetCurrentUserId();
            var bankAccount = await _unitOfWork.BankUsers.GetByIdAsync(bankUserId);

            if (bankAccount == null || bankAccount.userId != userId)
            {
                throw new NotFoundException("Không tìm thấy tài khoản ngân hàng.");
            }

            // soft delete
            bankAccount.isActive = false;

            if (bankAccount.isPrimary == true)
            {
                bankAccount.isPrimary = false;
            }

            await _unitOfWork.BankUsers.UpdateAsync(bankAccount);
            await _unitOfWork.CommitAsync();

            return true;
        }

        public async Task<bool> SetPrimaryBankAccountAsync(int bankUserId)
        {
            var userId = GetCurrentUserId();
            var bankAccount = await _unitOfWork.BankUsers.GetByIdAsync(bankUserId);

            if (bankAccount == null || bankAccount.userId != userId || bankAccount.isActive != true)
            {
                throw new NotFoundException("Không tìm thấy tài khoản ngân hàng.");
            }

            if (bankAccount.isPrimary == true) return true;

            await ResetPrimaryStatus(userId);

            bankAccount.isPrimary = true;
            await _unitOfWork.BankUsers.UpdateAsync(bankAccount);
            await _unitOfWork.CommitAsync();

            return true;
        }

        #region Private Methods

        private int GetCurrentUserId()
        {
            var userId = _userContextService.GetCurrentUserId();
            if (!userId.HasValue)
            {
                throw new UnauthorizedException("Không tìm thấy thông tin người dùng. Vui lòng đăng nhập lại.");
            }
            return userId.Value;
        }


        private async Task ResetPrimaryStatus(int userId)
        {
            var primaryAccount = await _unitOfWork.BankUsers.GetPrimaryByUserIdAsync(userId);
            if (primaryAccount != null)
            {
                primaryAccount.isPrimary = false;
                await _unitOfWork.BankUsers.UpdateAsync(primaryAccount);
            }
        }

        #endregion
    }
}