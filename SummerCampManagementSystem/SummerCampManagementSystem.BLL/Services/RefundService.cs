using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.BLL.DTOs.Refund;
using SummerCampManagementSystem.BLL.Exceptions;
using SummerCampManagementSystem.BLL.Helpers;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.Core.Enums;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;

namespace SummerCampManagementSystem.BLL.Services
{
    public class RefundService : IRefundService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IUserContextService _userContextService;
        private readonly IUploadSupabaseService _uploadService;

        public RefundService(IUnitOfWork unitOfWork, IMapper mapper, IUserContextService userContextService, IUploadSupabaseService uploadService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _userContextService = userContextService;
            _uploadService = uploadService;
        }

        public async Task<RefundCalculationDto> CalculateRefundAsync(int registrationId)
        {
            var userId = _userContextService.GetCurrentUserId();
            if (!userId.HasValue) throw new UnauthorizedException("User not authenticated.");

            // validation and get data
            var registration = await ValidateAndGetRegistrationForUserAsync(registrationId, userId.Value);

            return CalculateRefundInternal(registration);
        }

        public async Task<RefundCalculationDto> CalculateRefundForSystemAsync(int registrationId)
        {
            // reuse repository method - no user validation 
            var registration = await _unitOfWork.Registrations.GetWithDetailsForRefundAsync(registrationId);

            if (registration == null)
            {
                throw new NotFoundException($"Không tìm thấy đơn đăng ký với ID {registrationId}.");
            }

            return CalculateRefundInternal(registration);
        }

        public async Task<RegistrationCancelResponseDto> RequestCancelAsync(CancelRequestDto requestDto)
        {
            var userId = _userContextService.GetCurrentUserId();
            if (!userId.HasValue) throw new UnauthorizedException("User not authenticated.");

            // validation
            var registration = await ValidateRegistrationStatusForCancelAsync(requestDto.RegistrationId, userId.Value);

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

        public async Task<IEnumerable<RefundRequestListDto>> GetAllRefundRequestsAsync(RefundRequestFilterDto? filter = null)
        {
            var query = _unitOfWork.RegistrationCancels.GetQueryableWithDetails();

            if (filter != null)
            {
                if (filter.Status.HasValue)
                {
                    query = query.Where(rc => rc.status == filter.Status.Value.ToString());
                }
            }

            var requests = await query.OrderByDescending(rc => rc.requestDate).ToListAsync();

            return requests.Select(rc => new RefundRequestListDto
            {
                RegistrationCancelId = rc.registrationCancelId,
                RegistrationId = rc.registrationId ?? 0,

                // user information
                ParentName = rc.registration?.user != null ? $"{rc.registration.user.lastName} {rc.registration.user.firstName}" : "Unknown",
                ParentEmail = rc.registration?.user?.email ?? "",
                ParentPhone = rc.registration?.user?.phoneNumber ?? "",
                CamperNames = rc.registration?.RegistrationCampers.Select(cp => cp.camper?.camperName ?? "Unknown").ToList() ?? new List<string>(),

                // refund information
                RefundAmount = rc.refundAmount ?? 0,
                RequestDate = rc.requestDate.HasValue ? rc.requestDate.Value.ToVietnamTime() : DateTime.MinValue,
                Reason = rc.reason,
                Status = rc.status,
                ApprovalDate = rc.approvalDate.HasValue ? rc.approvalDate.Value.ToVietnamTime() : null,
                ManagerNote = rc.note,
                ImageRefund = rc.imageRefund,
                TransactionCode = rc.transactionCode,

                // bank information
                BankName = rc.bankUser?.bankName ?? "N/A",
                BankNumber = rc.bankUser?.bankNumber ?? "N/A",
                BankAccountName = rc.registration?.user != null ? $"{rc.registration.user.lastName} {rc.registration.user.firstName}" : ""
            });
        }

        public async Task<IEnumerable<RefundRequestListDto>> GetRefundRequestsByCampAsync(int campId, RefundRequestFilterDto? filter = null)
        {
            // validate camp existence
            var camp = await _unitOfWork.Camps.GetByIdAsync(campId);
            if (camp == null)
                throw new NotFoundException($"Không tìm thấy trại với ID {campId}.");

            // get refund requests for registrations of this camp
            var query = _unitOfWork.RegistrationCancels.GetQueryableWithDetails()
                .Where(rc => rc.registration.campId == campId);

            // apply filters if provided
            if (filter != null)
            {
                if (filter.Status.HasValue)
                {
                    query = query.Where(rc => rc.status == filter.Status.Value.ToString());
                }
            }

            var requests = await query.OrderByDescending(rc => rc.requestDate).ToListAsync();

            return requests.Select(rc => new RefundRequestListDto
            {
                RegistrationCancelId = rc.registrationCancelId,
                RegistrationId = rc.registrationId ?? 0,

                // user information
                ParentName = rc.registration?.user != null ? $"{rc.registration.user.lastName} {rc.registration.user.firstName}" : "Unknown",
                ParentEmail = rc.registration?.user?.email ?? "",
                ParentPhone = rc.registration?.user?.phoneNumber ?? "",
                CamperNames = rc.registration?.RegistrationCampers.Select(cp => cp.camper?.camperName ?? "Unknown").ToList() ?? new List<string>(),

                // refund information
                RefundAmount = rc.refundAmount ?? 0,
                RequestDate = rc.requestDate.HasValue ? rc.requestDate.Value.ToVietnamTime() : DateTime.MinValue,
                Reason = rc.reason,
                Status = rc.status,
                ApprovalDate = rc.approvalDate.HasValue ? rc.approvalDate.Value.ToVietnamTime() : null,
                ManagerNote = rc.note,
                ImageRefund = rc.imageRefund,
                TransactionCode = rc.transactionCode,

                // bank information
                BankName = rc.bankUser?.bankName ?? "N/A",
                BankNumber = rc.bankUser?.bankNumber ?? "N/A",
                BankAccountName = rc.registration?.user != null ? $"{rc.registration.user.lastName} {rc.registration.user.firstName}" : ""
            });
        }


        public async Task<RegistrationCancelResponseDto> ApproveRefundAsync(ApproveRefundDto dto)
        {
            // validaion
            var cancelRequest = await ValidateCancelRequestForManagerAsync(dto.RegistrationCancelId);

            // upload proof image
            string proofImageUrl = await _uploadService.UploadRefundProofAsync(cancelRequest.registrationCancelId, dto.RefundImage);
            if (string.IsNullOrEmpty(proofImageUrl)) throw new Exception("Lỗi upload ảnh minh chứng.");

            // update cancel request
            cancelRequest.imageRefund = proofImageUrl;
            cancelRequest.transactionCode = dto.TransactionCode;
            cancelRequest.note = dto.ManagerNote;
            cancelRequest.status = RegistrationCancelStatus.Completed.ToString();
            cancelRequest.approvalDate = DateTime.UtcNow;

            await _unitOfWork.RegistrationCancels.UpdateAsync(cancelRequest);

            await CreateRefundTransactionAsync(cancelRequest);

            var registration = cancelRequest.registration;
            if (registration != null)
            {
                registration.status = RegistrationStatus.Refunded.ToString();
                await _unitOfWork.Registrations.UpdateAsync(registration);

                // update all RegistrationCampers to Canceled status
                var registrationCampers = await _unitOfWork.RegistrationCampers
                    .GetQueryable()
                    .Where(rc => rc.registrationId == registration.registrationId)
                    .ToListAsync();

                foreach (var regCamper in registrationCampers)
                {
                    regCamper.status = RegistrationCamperStatus.Canceled.ToString();
                    await _unitOfWork.RegistrationCampers.UpdateAsync(regCamper);
                }

                // update all CamperTransports to Canceled status
                var camperIds = registrationCampers.Select(rc => rc.camperId).ToList();
                if (camperIds.Any())
                {
                    var camperTransports = await _unitOfWork.CamperTransports
                        .GetQueryable()
                        .Where(ct => camperIds.Contains(ct.camperId) && 
                                     ct.transportSchedule.campId == registration.campId)
                        .ToListAsync();

                    foreach (var transport in camperTransports)
                    {
                        transport.status = CamperTransportStatus.Canceled.ToString();
                        await _unitOfWork.CamperTransports.UpdateAsync(transport);
                    }
                }
            }

            // 6. [TODO] Release Resources (Phase 5)

            await _unitOfWork.CommitAsync();

            var response = _mapper.Map<RegistrationCancelResponseDto>(cancelRequest);
            response.Status = cancelRequest.status;
            return response;
        }

        public async Task<RegistrationCancelResponseDto> RejectRefundAsync(RejectRefundDto dto)
        {
            // validation
            var cancelRequest = await ValidateCancelRequestForManagerAsync(dto.RegistrationCancelId);

            // update cancel request status
            cancelRequest.status = RegistrationCancelStatus.Rejected.ToString();
            cancelRequest.note = dto.RejectReason;
            cancelRequest.approvalDate = DateTime.UtcNow;

            await _unitOfWork.RegistrationCancels.UpdateAsync(cancelRequest);

            // cancel registration even though refund rejected
            var registration = cancelRequest.registration;
            if (registration != null)
            {
                registration.status = RegistrationStatus.Canceled.ToString();
                await _unitOfWork.Registrations.UpdateAsync(registration);

                // update all RegistrationCampers to Canceled 
                var registrationCampers = await _unitOfWork.RegistrationCampers
                    .GetQueryable()
                    .Where(rc => rc.registrationId == registration.registrationId)
                    .ToListAsync();

                foreach (var regCamper in registrationCampers)
                {
                    regCamper.status = RegistrationCamperStatus.Canceled.ToString();
                    await _unitOfWork.RegistrationCampers.UpdateAsync(regCamper);
                }

                // update all CamperTransports to Canceled
                var camperIds = registrationCampers.Select(rc => rc.camperId).ToList();
                if (camperIds.Any())
                {
                    var camperTransports = await _unitOfWork.CamperTransports
                        .GetQueryable()
                        .Where(ct => camperIds.Contains(ct.camperId) && 
                                     ct.transportSchedule.campId == registration.campId)
                        .ToListAsync();

                    foreach (var transport in camperTransports)
                    {
                        transport.status = CamperTransportStatus.Canceled.ToString();
                        await _unitOfWork.CamperTransports.UpdateAsync(transport);
                    }
                }
            }

            await _unitOfWork.CommitAsync();

            var response = _mapper.Map<RegistrationCancelResponseDto>(cancelRequest);
            response.Status = cancelRequest.status;
            return response;
        }

        #region Private Methods

        private async Task<Registration> ValidateAndGetRegistrationForUserAsync(int registrationId, int userId)
        {
            var registration = await _unitOfWork.Registrations.GetWithDetailsForRefundAsync(registrationId);

            if (registration == null)
                throw new NotFoundException($"Không tìm thấy đơn đăng ký ID {registrationId}.");

            if (registration.userId != userId)
                throw new UnauthorizedException("Bạn không có quyền thao tác trên đơn đăng ký này.");

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

        // check registration status for cancel request
        private async Task<Registration> ValidateRegistrationStatusForCancelAsync(int registrationId, int userId)
        {
            var registration = await ValidateAndGetRegistrationForUserAsync(registrationId, userId);

            // check if registration period is closed
            var camp = registration.camp;
            if (camp != null && camp.registrationEndDate.HasValue)
            {
                var currentDate = DateTime.UtcNow;
                if (currentDate >= camp.registrationEndDate.Value)
                {
                    throw new RefundPolicyViolationException("Không thể hủy đăng ký sau khi thời gian đăng ký đã đóng.");
                }
            }

            if (registration.status == RegistrationStatus.PendingRefund.ToString())
                throw new InvalidRefundRequestException("Yêu cầu hủy cho đơn này đang được xử lý.");

            if (registration.status != RegistrationStatus.Confirmed.ToString() &&
                registration.status != RegistrationStatus.PendingPayment.ToString())
            {
                throw new InvalidRefundRequestException($"Không thể hủy đơn ở trạng thái '{registration.status}'.");
            }

            return registration;
        }

        private async Task ValidateBankInfoIfPaidAsync(Registration registration, int bankUserId, int userId)
        {
            bool isPaid = registration.status == RegistrationStatus.Confirmed.ToString();

            if (isPaid)
            {
                var bankUser = await _unitOfWork.BankUsers.GetByIdAsync(bankUserId);
                // check if bank info is valid
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


        // MANAGER

        private async Task<RegistrationCancel> ValidateCancelRequestForManagerAsync(int cancelId)
        {
            var cancelRequest = await _unitOfWork.RegistrationCancels.GetByIdWithDetailsAsync(cancelId);

            if (cancelRequest == null)
                throw new NotFoundException($"Không tìm thấy yêu cầu hủy ID {cancelId}.");

            if (cancelRequest.status != RegistrationCancelStatus.Pending.ToString())
                throw new InvalidOperationException($"Yêu cầu này đang ở trạng thái '{cancelRequest.status}', không thể thao tác.");

            if (cancelRequest.registration == null)
                throw new NotFoundException("Dữ liệu lỗi: Đơn đăng ký gốc không tồn tại.");

            return cancelRequest;
        }

        private async Task CreateRefundTransactionAsync(RegistrationCancel request)
        {
            var refundTransaction = new Transaction
            {
                registrationId = request.registrationId,
                amount = Math.Abs(request.refundAmount ?? 0), 
                type = "Refund",
                status = TransactionStatus.Confirmed.ToString(),
                method = "BankTransfer",
                transactionCode = $"REFUND-{request.transactionCode}",
                transactionTime = DateTime.UtcNow
            };

            await _unitOfWork.Transactions.CreateAsync(refundTransaction);
        }


        #endregion
    }
}