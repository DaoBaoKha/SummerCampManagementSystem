using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.BLL.DTOs.Refund;
using SummerCampManagementSystem.BLL.Exceptions;
using SummerCampManagementSystem.BLL.Helpers;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.Core.Enums;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;
using Supabase.Gotrue;

namespace SummerCampManagementSystem.BLL.Services
{
    public class RefundService : IRefundService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IUserContextService _userContextService;

        public RefundService(IUnitOfWork unitOfWork, IMapper mapper, IUserContextService userContextService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _userContextService = userContextService;
        }

        public async Task<RefundCalculationDto> CalculateRefundAsync(int registrationId)
        {
            var userId = _userContextService.GetCurrentUserId();
            if (!userId.HasValue) throw new UnauthorizedException("User not authenticated.");

            var registration = await GetRegistrationWithDetailsAsync(registrationId, userId.Value);

            return CalculateRefundInternal(registration);
        }

        public async Task<RegistrationCancelResponseDto> RequestCancelAsync(CancelRequestDto requestDto)
        {
            var userId = _userContextService.GetCurrentUserId();
            if (!userId.HasValue) throw new UnauthorizedException("User not authenticated.");

            // validation
            var registration = await ValidateRegistrationForCancelAsync(requestDto.RegistrationId, userId.Value);

            await ValidateBankInfoIfPaidAsync(registration, requestDto.BankUserId, userId.Value);

            // calculate refund
            var refundInfo = CalculateRefundInternal(registration);

            // create cancel request
            var cancelRequest = await CreateCancelRequestAsync(requestDto, refundInfo, registration.status == RegistrationStatus.Confirmed.ToString());

            await UpdateRegistrationStatusAsync(registration, cancelRequest.status);

            return new RegistrationCancelResponseDto
            {
                RegistrationCancelId = cancelRequest.registrationCancelId,
                RegistrationId = cancelRequest.registrationId.Value,
                RefundAmount = cancelRequest.refundAmount ?? 0,
                RequestDate = cancelRequest.requestDate ?? DateTime.UtcNow,
                Status = cancelRequest.status,
                Reason = cancelRequest.reason
            };
        }

        #region Private Methods

        // get registration with realated data
        private async Task<Registration> GetRegistrationWithDetailsAsync(int registrationId, int userId)
        {
            var registration = await _unitOfWork.Registrations.GetQueryable()
                .Include(r => r.camp)
                .Include(r => r.Transactions)
                .FirstOrDefaultAsync(r => r.registrationId == registrationId);

            if (registration == null)
                throw new NotFoundException($"Không tìm thấy đơn đăng ký ID {registrationId}.");

            if (registration.userId != userId)
            {
                throw new UnauthorizedException("Bạn không có quyền xem hoặc thao tác trên đơn đăng ký này.");
            }

            return registration;
        }

        private RefundCalculationDto CalculateRefundInternal(Registration registration)
        {
            // havent paid -> free cancel
            if (registration.status == RegistrationStatus.PendingPayment.ToString())
            {
                return new RefundCalculationDto
                {
                    TotalAmountPaid = 0,
                    RefundAmount = 0,
                    RefundPercentage = 0,
                    PolicyDescription = "Đơn chưa thanh toán (Hủy miễn phí)."
                };
            }

            // already paid -> calculate refund
            var totalPaid = registration.Transactions
                .Where(t => t.type == "Payment" && t.status == TransactionStatus.Confirmed.ToString())
                .Sum(t => t.amount) ?? 0;

            if (totalPaid <= 0)
            {
                return new RefundCalculationDto
                {
                    TotalAmountPaid = 0,
                    RefundAmount = 0,
                    RefundPercentage = 0,
                    PolicyDescription = "Chưa ghi nhận khoản thanh toán nào."
                };
            }

            // calculate % refund based on camp dates
            var camp = registration.camp;
            if (camp.startDate == null)
                throw new InvalidOperationException("Trại chưa có ngày bắt đầu");

            var campStartDate = camp.startDate.Value;
            var registrationEndDate = camp.registrationEndDate ?? campStartDate; // if no reg end date use start date

            var requestDate = DateTime.UtcNow;

            int refundPercentage = 0;
            string policyDesc = "";

            // if camp already started -> no refund
            if (requestDate >= campStartDate)
            {
                refundPercentage = 0;
                policyDesc = "Trại đã bắt đầu (Không hoàn tiền).";
            }
            else
            {
                // camp not begin yet
                if (requestDate < registrationEndDate)
                {
                    // before registration end date -> refund 100%
                    refundPercentage = 100;
                    policyDesc = "Hủy trước khi đóng đăng ký (Hoàn 100%).";
                }
                else
                {
                    // after registration end date and before camp start -> refund 50%
                    refundPercentage = 50;
                    policyDesc = "Hủy sau khi đóng đăng ký (Hoàn 50%).";
                }
            }

            decimal refundAmount = totalPaid * refundPercentage / 100;

            return new RefundCalculationDto
            {
                TotalAmountPaid = totalPaid,
                RefundAmount = refundAmount,
                RefundPercentage = refundPercentage,
                PolicyDescription = policyDesc
            };
        }

        // check registration cancel
        private async Task<Registration> ValidateRegistrationForCancelAsync(int registrationId, int userId)
        {
            var registration = await GetRegistrationWithDetailsAsync(registrationId, userId);

            if (registration.status == RegistrationStatus.PendingRefund.ToString())
                throw new InvalidRefundRequestException("Yêu cầu hủy cho đơn này đang được xử lý.");

            if (registration.status != RegistrationStatus.Confirmed.ToString() &&
                registration.status != RegistrationStatus.PendingPayment.ToString())
            {
                throw new InvalidRefundRequestException($"Không thể hủy đơn ở trạng thái '{registration.status}'.");
            }

            return registration;
        }

        // check bank info if paid
        private async Task ValidateBankInfoIfPaidAsync(Registration registration, int bankUserId, int userId)
        {
            bool isPaid = registration.status == RegistrationStatus.Confirmed.ToString();

            if (isPaid)
            {
                var bankUser = await _unitOfWork.BankUsers.GetByIdAsync(bankUserId);
                // Kiểm tra bank có tồn tại, thuộc về user và isActive == true
                if (bankUser == null || bankUser.userId != userId || bankUser.isActive != true)
                    throw new BadRequestException("Thông tin tài khoản ngân hàng không hợp lệ.");
            }
        }

        private async Task<RegistrationCancel> CreateCancelRequestAsync(CancelRequestDto requestDto, RefundCalculationDto refundInfo, bool isPaid)
        {
            var cancelRequest = _mapper.Map<RegistrationCancel>(requestDto);

            cancelRequest.refundAmount = refundInfo.RefundAmount;
            cancelRequest.requestDate = DateTime.UtcNow;
            cancelRequest.bankUserId = isPaid ? requestDto.BankUserId : null;

            // if refund amount is 0 -> Completed
            // else -> Pending
            cancelRequest.status = (refundInfo.RefundAmount == 0)
                         ? RegistrationCancelStatus.Completed.ToString()
                         : RegistrationCancelStatus.Pending.ToString();

            await _unitOfWork.RegistrationCancels.CreateAsync(cancelRequest);
            return cancelRequest;
        }

        // update registration status after cancel request
        private async Task UpdateRegistrationStatusAsync(Registration registration, string cancelRequestStatus)
        {
            if (cancelRequestStatus == RegistrationCancelStatus.Completed.ToString())
            {
                registration.status = RegistrationStatus.Canceled.ToString();
                // TODO: ReleaseResourcesAsync(registration.registrationId) 
            }
            else
            {
                registration.status = RegistrationStatus.PendingRefund.ToString();
            }

            await _unitOfWork.Registrations.UpdateAsync(registration);
            await _unitOfWork.CommitAsync();
        }

        #endregion
    }
}