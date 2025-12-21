using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SummerCampManagementSystem.BLL.DTOs.Camp;
using SummerCampManagementSystem.BLL.Exceptions;
using SummerCampManagementSystem.BLL.Helpers;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.Core.Enums;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;

namespace SummerCampManagementSystem.BLL.Services
{
    public class CampService : ICampService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IUserContextService _userContextService;
        private readonly ILogger<CampService> _logger;

        public CampService(IUnitOfWork unitOfWork, IMapper mapper, IUserContextService userContextService, ILogger<CampService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _userContextService = userContextService;
            _logger = logger;
        }

        public async Task<CampResponseDto> CreateCampAsync(CampRequestDto campRequest)
        {
            // check validation before creating
            await RunValidationChecks(campRequest);

            // map the request DTO to the Camp entity
            var newCamp = _mapper.Map<Camp>(campRequest);

            // get userId
            newCamp.createBy = _userContextService.GetCurrentUserId();
            if (newCamp.createBy == null)
            {
                throw new UnauthorizedAccessException("Người dùng hiện tại không hợp lệ hoặc chưa đăng nhập.");
            }

            newCamp.status = CampStatus.Draft.ToString();

            if (newCamp.startDate.HasValue) newCamp.startDate = newCamp.startDate.Value.ToUtcForStorage();
            if (newCamp.endDate.HasValue) newCamp.endDate = newCamp.endDate.Value.ToUtcForStorage();
            if (newCamp.registrationStartDate.HasValue) newCamp.registrationStartDate = newCamp.registrationStartDate.Value.ToUtcForStorage();
            if (newCamp.registrationEndDate.HasValue) newCamp.registrationEndDate = newCamp.registrationEndDate.Value.ToUtcForStorage();

            newCamp.createdAt = DateTime.UtcNow;


            await _unitOfWork.Camps.CreateAsync(newCamp);
            await _unitOfWork.CommitAsync();

            // get the created camp with related entities
            var createdCamp = await GetCampsWithIncludes()
                .FirstOrDefaultAsync(c => c.campId == newCamp.campId);

            if (createdCamp == null)
            {
                throw new Exception("Failed to retrieve the created camp for mapping.");
            }

            return _mapper.Map<CampResponseDto>(createdCamp);
        }

        public async Task<bool> DeleteCampAsync(int id)
        {
            var existingCamp = await _unitOfWork.Camps.GetByIdAsync(id);
            if (existingCamp == null) return false;
            await _unitOfWork.Camps.RemoveAsync(existingCamp);
            await _unitOfWork.CommitAsync();

            return true;
        }

        public async Task<IEnumerable<CampResponseDto>> GetAllCampsAsync()
        {
            var camps = await GetCampsWithIncludes().ToListAsync();

            return _mapper.Map<IEnumerable<CampResponseDto>>(camps);
        }

        public async Task<IEnumerable<CampResponseDto>> GetCampsByStaffIdAsync(int staffId)
        {
            var staff = await _unitOfWork.Users.GetByIdAsync(staffId)
                ?? throw new NotFoundException($"Staff with ID {staffId} not found.");

            var camps = await _unitOfWork.Camps.GetCampsByStaffIdAsync(staffId)
                ?? throw new NotFoundException($"No camps found for staff ID {staffId}.");

            return _mapper.Map<IEnumerable<CampResponseDto>>(camps);
        }

        public async Task<CampResponseDto?> GetCampByIdAsync(int id)
        {
            var camp = await GetCampsWithIncludes()
                .FirstOrDefaultAsync(c => c.campId == id);

            if (camp == null)
            {
                return null;
            }

            var campResponse = _mapper.Map<CampResponseDto>(camp);

            // calculate current capacity by counting confirmed campers
            var currentCapacity = await _unitOfWork.RegistrationCampers.GetQueryable()
                .CountAsync(rc => rc.registration.campId == id 
                                  && rc.status == RegistrationCamperStatus.Confirmed.ToString());

            campResponse.CurrentCapacity = currentCapacity;

            return campResponse;
        }

        public async Task<IEnumerable<CampResponseDto>> GetCampsByTypeAsync(int campTypeId)
        {
            var camps = await GetCampsWithIncludes()
                .Where(c => c.campTypeId == campTypeId)
                .ToListAsync();

            if (camps == null || !camps.Any())
            {
                return Enumerable.Empty<CampResponseDto>();
            }

            return _mapper.Map<IEnumerable<CampResponseDto>>(camps);
        }

        public async Task<CampResponseDto> TransitionCampStatusAsync(int campId, CampStatus newStatus)
        {
            var existingCamp = await GetCampsWithIncludes()
                .FirstOrDefaultAsync(c => c.campId == campId);

            if (existingCamp == null)
            {
                throw new NotFoundException($"Camp with ID {campId} not found.");
            }

            if (!Enum.TryParse(existingCamp.status, true, out CampStatus currentStatus))
            {
                throw new BusinessRuleException("Trạng thái hiện tại của Camp không hợp lệ.");
            }

            bool isValidTransition = false;

            // cancel status
            if (newStatus == CampStatus.Canceled)
            {
                // allow cancel before InProgress and Completed
                // (Draft, Pending, Rejected, Published, OpenForRegistration, RegistrationClosed, UnderEnrolled)
                if (currentStatus < CampStatus.InProgress && currentStatus != CampStatus.Canceled)
                {
                    isValidTransition = true;
                }
            }
            // switch
            else
            {
                switch (currentStatus)
                {
                    case CampStatus.Draft:
                        // Draft -> PendingApproval use SubmitForApprovalAsync
                        if (newStatus == CampStatus.PendingApproval)
                        {
                            // if use  Transition directly (should use SubmitForApproval)
                            isValidTransition = true;
                        }
                        break;

                    case CampStatus.PendingApproval:
                        // admin Approve/Reject
                        if (newStatus == CampStatus.Published || newStatus == CampStatus.Rejected)
                        {
                            isValidTransition = true;
                        }
                        break;

                    case CampStatus.Rejected:
                        // Manager adjust (back to PendingApproval)
                        if (newStatus == CampStatus.PendingApproval)
                        {
                            isValidTransition = true;
                        }
                        break;

                    case CampStatus.Published:
                        // system check time
                        if (newStatus == CampStatus.OpenForRegistration)
                        {
                            isValidTransition = true;
                        }
                        break;

                    case CampStatus.OpenForRegistration:
                        // system check time (-> Closed or UnderEnrolled)
                        if (newStatus == CampStatus.RegistrationClosed || newStatus == CampStatus.UnderEnrolled)
                        {
                            isValidTransition = true;
                        }
                        break;

                    case CampStatus.RegistrationClosed:
                        // camp start time (-> InProgress)
                        if (newStatus == CampStatus.InProgress)
                        {
                            isValidTransition = true;
                        }
                        // if not enough campers -> UnderEnrolled (System check)
                        else if (newStatus == CampStatus.UnderEnrolled)
                        {
                            isValidTransition = true;
                        }
                        break;

                    case CampStatus.UnderEnrolled:
                        // Admin extend registration time (-> OpenForRegistration)
                        if (newStatus == CampStatus.OpenForRegistration)
                        {
                            isValidTransition = true;
                        }
                        // or admin start -> InProgress
                        else if (newStatus == CampStatus.InProgress)
                        {
                            isValidTransition = true;
                        }
                        break;


                    case CampStatus.InProgress:
                        // camp end
                        if (newStatus == CampStatus.Completed)
                        {
                            isValidTransition = true;
                        }
                        break;

                    case CampStatus.Completed:
                    case CampStatus.Canceled:
                        // Final states
                        break;
                }
            }

            if (!isValidTransition)
            {
                throw new BadRequestException($"Chuyển đổi trạng thái từ '{currentStatus}' sang '{newStatus}' không hợp lệ theo flow đã quy định.");
            }

            if (newStatus == CampStatus.Published) // approve other schedules
            {
                await ApproveCampAndSchedulesAsync(campId);
            }
            else if (newStatus == CampStatus.Rejected) // reject other schedules
            {
                await RejectCampAndSchedulesAsync(campId);
            }

            existingCamp.status = newStatus.ToString();

            await _unitOfWork.Camps.UpdateAsync(existingCamp);
            await _unitOfWork.CommitAsync();

            return _mapper.Map<CampResponseDto>(existingCamp);
        }


        public async Task<CampResponseDto> UpdateCampAsync(int campId, CampRequestDto campRequest)
        {
            // get the existing camp with related entities
            var existingCamp = await GetCampsWithIncludes().FirstOrDefaultAsync(c => c.campId == campId);

            if (existingCamp == null)
            {
                throw new NotFoundException("Camp not found");
            }

            bool isUnderEnrolled = existingCamp.status == CampStatus.UnderEnrolled.ToString();

            // only update when status = Draft, Rejected, UnderEnrolled
            if (existingCamp.status != CampStatus.Draft.ToString() &&
                existingCamp.status != CampStatus.Rejected.ToString() &&
                !isUnderEnrolled)
            {
                throw new BadRequestException($"Camp chỉ có thể chỉnh sửa khi ở trạng thái Draft, Rejected hoặc UnderEnrolled. Trạng thái hiện tại: {existingCamp.status}");
            }

            await RunValidationChecks(campRequest, campId);

            _mapper.Map(campRequest, existingCamp);

            if (existingCamp.startDate.HasValue) existingCamp.startDate = existingCamp.startDate.Value.ToUtcForStorage();
            if (existingCamp.endDate.HasValue) existingCamp.endDate = existingCamp.endDate.Value.ToUtcForStorage();
            if (existingCamp.registrationStartDate.HasValue) existingCamp.registrationStartDate = existingCamp.registrationStartDate.Value.ToUtcForStorage();
            if (existingCamp.registrationEndDate.HasValue) existingCamp.registrationEndDate = existingCamp.registrationEndDate.Value.ToUtcForStorage();

            if (existingCamp.status == CampStatus.Rejected.ToString())
            {
                existingCamp.status = CampStatus.Draft.ToString(); // back to draft after updating
            }

            // if underEnrolled -> OpenForRegistration 
            else if (isUnderEnrolled)
            {
                if (existingCamp.registrationEndDate.Value > DateTime.UtcNow)
                {
                    existingCamp.status = CampStatus.OpenForRegistration.ToString();
                }
            }

            await _unitOfWork.Camps.UpdateAsync(existingCamp);
            await _unitOfWork.CommitAsync();

            return _mapper.Map<CampResponseDto>(existingCamp);
        }

        public async Task<CampResponseDto> UpdateCampStatusAsync(int campId, CampStatusUpdateRequestDto statusUpdate)
        {
            return await TransitionCampStatusAsync(campId, statusUpdate.Status);
        }

        public async Task<IEnumerable<CampResponseDto>> GetCampsByStatusAsync(CampStatus? status = null)
        {
            var query = GetCampsWithIncludes();

            if (status.HasValue)
            {
                string statusString = status.Value.ToString();

                query = query.Where(c => c.status == statusString);
            }
            // if status is null, return all camps

            var camps = await query.ToListAsync();

            return _mapper.Map<IEnumerable<CampResponseDto>>(camps);
        }

        public async Task<IEnumerable<CampResponseDto>> GetActiveCampsAsync()
        {
            var activeStatuses = new[] {
                CampStatus.Published.ToString(),
                CampStatus.OpenForRegistration.ToString(),
                CampStatus.RegistrationClosed.ToString(),
                CampStatus.InProgress.ToString()
            };

            var camps = await GetCampsWithIncludes()
                .Where(c => activeStatuses.Contains(c.status))
                .ToListAsync();

            return _mapper.Map<IEnumerable<CampResponseDto>>(camps);
        }

        // change status Draft to PendingApproval and check required conditions
        public async Task<CampResponseDto> SubmitForApprovalAsync(int campId)
        {
            var existingCamp = await GetCampsWithIncludes()
                .FirstOrDefaultAsync(c => c.campId == campId) ?? throw new Exception($"Camp with ID {campId} not found.");

            if (!Enum.TryParse(existingCamp.status, true, out CampStatus currentStatus) ||
                (currentStatus != CampStatus.Draft && currentStatus != CampStatus.Rejected))
            {
                throw new BadRequestException($"Camp hiện tại đang ở trạng thái '{currentStatus}'. Chỉ có thể gửi phê duyệt từ trạng thái Draft hoặc Rejected.");
            }


            // CampStaffAssignments check Group/Staff. 
            // Activities check activity.

            // This section checks Activity and Group/Staff Assignment.

            var hasActivities = await _unitOfWork.Activities.GetQueryable()
                .AnyAsync(a => a.campId == campId);

            var hasGroupsOrStaff = await _unitOfWork.Camps.GetQueryable()
                .Where(c => c.campId == campId)
                .Select(c => c.CampStaffAssignments.Any() || c.Groups.Any()) // check staff or group
                .FirstOrDefaultAsync();

            if (!hasActivities)
            {
                throw new BadRequestException("Không thể gửi phê duyệt. Camp cần có ít nhất một hoạt động (Activity) được tạo.");
            }
            if (!hasGroupsOrStaff)
            {
                throw new BadRequestException("Không thể gửi phê duyệt. Camp cần có ít nhất một Group/Staff Assignment hoặc Camper Group được tạo.");
            }

            // if camp was previously rejected, reset rejected schedules to draft
            if (currentStatus == CampStatus.Rejected)
            {
                await ResetRejectedSchedulesToDraftAsync(campId);
            }

            return await TransitionCampStatusAsync(campId, CampStatus.PendingApproval);
        }

    
        public async Task<CampResponseDto> RejectCampAsync(int campId, CampRejectRequestDto request)
    {
        // get existing camp
        var existingCamp = await GetCampsWithIncludes()
            .FirstOrDefaultAsync(c => c.campId == campId) ?? throw new NotFoundException($"Không tìm thấy Camp với ID {campId}.");

        // validate current status
        if (!Enum.TryParse(existingCamp.status, true, out CampStatus currentStatus) ||
            currentStatus != CampStatus.PendingApproval)
        {
            throw new BadRequestException($"Camp hiện tại đang ở trạng thái '{currentStatus}'. Chỉ có thể từ chối phê duyệt khi trại đang ở trạng thái PendingApproval.");
        }

        // transition status and reject related schedules first
        var result = await TransitionCampStatusAsync(campId, CampStatus.Rejected);
        
        // save reject note after transition
        var campToUpdate = await _unitOfWork.Camps.GetByIdAsync(campId);
        if (campToUpdate != null)
        {
            campToUpdate.note = request.Note;
            await _unitOfWork.Camps.UpdateAsync(campToUpdate);
            await _unitOfWork.CommitAsync();
            
            // update result with the note
            result.Note = request.Note;
        }

        return result;
    }

        public async Task<CampResponseDto> CancelCampAsync(int campId, CampCancelRequestDto request)
    {
        // get existing camp
        var existingCamp = await GetCampsWithIncludes()
            .FirstOrDefaultAsync(c => c.campId == campId) ?? throw new NotFoundException($"Không tìm thấy Camp với ID {campId}.");

        // validate current status - can cancel anytime except Completed or already Canceled
        if (!Enum.TryParse(existingCamp.status, true, out CampStatus currentStatus))
        {
            throw new BadRequestException("Trạng thái hiện tại của Camp không hợp lệ.");
        }

        if (currentStatus == CampStatus.Completed)
        {
            throw new BadRequestException("Không thể hủy trại đã hoàn thành.");
        }

        if (currentStatus == CampStatus.Canceled)
        {
            throw new BadRequestException("Trại này đã được hủy trước đó.");
        }

        // cancel all related entities first
        await CancelCampAndRelatedEntitiesAsync(campId);

        // transition camp status to Canceled
        var result = await TransitionCampStatusAsync(campId, CampStatus.Canceled);

        // save cancel note after transition
        var campToUpdate = await _unitOfWork.Camps.GetByIdAsync(campId);
        if (campToUpdate != null)
        {
            campToUpdate.note = request.Note;
            await _unitOfWork.Camps.UpdateAsync(campToUpdate);
            await _unitOfWork.CommitAsync();
            
            // update result with the note
            result.Note = request.Note;
        }

        return result;
    }

        public async Task<CampResponseDto> ExtendRegistrationAsync(int campId, DateTime newRegistrationEndDate)
        {
            var camp = await GetCampsWithIncludes().FirstOrDefaultAsync(c => c.campId == campId) ?? throw new NotFoundException($"Không tìm thấy Camp với ID {campId}.");

            // only allow when status = UnderEnrolled
            if (camp.status != CampStatus.UnderEnrolled.ToString())
            {
                throw new BadRequestException($"Chỉ có thể gia hạn khi trại đang ở trạng thái 'UnderEnrolled'. Trạng thái hiện tại: {camp.status}.");
            }

            DateTime newEndDateUtc = newRegistrationEndDate.ToUtcForStorage();

            // validate
            if (newEndDateUtc <= DateTime.UtcNow)
            {
                throw new BadRequestException("Thời gian đóng đăng ký mới phải là một thời điểm trong tương lai.");
            }

            // validate buffer time
            if (camp.startDate.HasValue)
            {
                // isCreateNew = false -> allow 5 days buffer
                ValidateRegistrationBufferTime(camp.startDate.Value, newEndDateUtc, isCreateNew: false);
            }

            camp.registrationEndDate = newEndDateUtc;
            camp.status = CampStatus.OpenForRegistration.ToString();

            await _unitOfWork.Camps.UpdateAsync(camp);
            await _unitOfWork.CommitAsync();

            _logger.LogInformation($"Admin extended registration for Camp {campId} to {newEndDateUtc}. Status -> OpenForRegistration.");

            return _mapper.Map<CampResponseDto>(camp);
        }

        public async Task<CampResponseDto> UpdateCampStatusNoValidationAsync(int campId, CampStatus newStatus)
        {
            var existingCamp = await _unitOfWork.Camps.GetByIdAsync(campId);

            if (existingCamp == null)
            {
                throw new NotFoundException($"Camp with ID {campId} not found.");
            }

            existingCamp.status = newStatus.ToString();

            await _unitOfWork.Camps.UpdateAsync(existingCamp);
            await _unitOfWork.CommitAsync();

            // get updated camp with includes
            var updatedCamp = await GetCampsWithIncludes()
                .FirstOrDefaultAsync(c => c.campId == campId);

            return _mapper.Map<CampResponseDto>(updatedCamp);
        }

        #region Scheduled Status Transitions

        public async Task RunScheduledStatusTransitionsAsync()
        {
            _logger.LogInformation("Starting scheduled camp status transition job.");

            // get camp
            var pendingCamps = await _unitOfWork.Camps.GetQueryable()
                .Where(c => c.status == CampStatus.Published.ToString() ||
                            c.status == CampStatus.OpenForRegistration.ToString() ||
                            c.status == CampStatus.RegistrationClosed.ToString() ||
                            c.status == CampStatus.InProgress.ToString())
                .ToListAsync();

            _logger.LogInformation($"Found {pendingCamps.Count} camps pending status check.");

            DateTime utcNow = DateTime.UtcNow;

            foreach (var camp in pendingCamps)
            {
                try
                {
                    if (!Enum.TryParse(camp.status, true, out CampStatus currentStatus))
                    {
                        _logger.LogWarning($"Camp ID {camp.campId} has invalid status string: '{camp.status}'. Skipping.");
                        continue;
                    }

                    CampStatus? nextStatus = null;
                    //_logger.LogDebug($"Checking Camp ID {camp.campId}. Current Status: {currentStatus}.");

                    // Published -> OpenForRegistration
                    if (currentStatus == CampStatus.Published &&
                        camp.registrationStartDate.HasValue &&
                        camp.registrationStartDate.Value <= utcNow)
                    {
                        nextStatus = CampStatus.OpenForRegistration;
                        _logger.LogInformation($"Camp {camp.campId}: Registration Start Date reached ({camp.registrationStartDate}). Prepared to Open.");
                    }

                    // OpenForRegistration -> registrationEndDate -> check min participants
                    else if (currentStatus == CampStatus.OpenForRegistration &&
                             camp.registrationEndDate.HasValue &&
                             camp.registrationEndDate.Value <= utcNow)
                    {
                        // count confirmed campers
                        int confirmedCount = await _unitOfWork.RegistrationCampers.GetQueryable()
                            .CountAsync(rc => rc.registration.campId == camp.campId
                                           && rc.status == RegistrationCamperStatus.Confirmed.ToString());

                        int minRequired = camp.minParticipants ?? 0;

                        _logger.LogInformation($"Camp {camp.campId}: Registration End Date reached. Checking participants: Confirmed={confirmedCount}, MinRequired={minRequired}.");

                        if (confirmedCount < minRequired)
                        {
                            // NOT ENOUGH CAMPERS -> UNDER ENROLLED 
                            nextStatus = CampStatus.UnderEnrolled;
                            _logger.LogWarning($"Camp {camp.campId} UnderEnrolled! Confirmed ({confirmedCount}) < Min ({minRequired}). Transitioning to UnderEnrolled.");
                        }
                        else
                        {
                            // enough campers -> RegistrationClosed
                            nextStatus = CampStatus.RegistrationClosed;
                            _logger.LogInformation($"Camp {camp.campId} met participant requirements. Transitioning to RegistrationClosed.");
                        }
                    }

                    // RegistrationClosed -> InProgress
                    else if (currentStatus == CampStatus.RegistrationClosed &&
                             camp.startDate.HasValue &&
                             camp.startDate.Value <= utcNow)
                    {
                        nextStatus = CampStatus.InProgress;
                        _logger.LogInformation($"Camp {camp.campId}: Start Date reached ({camp.startDate}). Transitioning to InProgress.");
                    }

                    // InProgress -> Completed
                    else if (currentStatus == CampStatus.InProgress &&
                             camp.endDate.HasValue &&
                             camp.endDate.Value <= utcNow)
                    {
                        nextStatus = CampStatus.Completed;
                        _logger.LogInformation($"Camp {camp.campId}: End Date reached ({camp.endDate}). Transitioning to Completed.");
                    }

                    // continue to next camp if no status change
                    if (nextStatus.HasValue)
                    {
                        await TransitionCampStatusAsync(camp.campId, nextStatus.Value);
                        _logger.LogInformation($"SUCCESS: Camp {camp.campId} transitioned from {currentStatus} to {nextStatus}.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"CRITICAL ERROR processing status transition for Camp ID {camp.campId}");
                }
            }

            _logger.LogInformation("Completed scheduled camp status transition job.");
        }

        #endregion

        #region Private Methods

        // help method to include related entities
        private IQueryable<Camp> GetCampsWithIncludes()
        {
            //load related entities
            return _unitOfWork.Camps.GetQueryable()
                .Include(c => c.campType)
                .Include(c => c.location)
                .Include(c => c.promotion);
        }

        private void ValidateRegistrationBufferTime(DateTime startDate, DateTime regEndDate, bool isCreateNew)
        {
            TimeSpan buffer = startDate.Date - regEndDate.Date;

            if (isCreateNew) // create new camp 
            {
                if (buffer.TotalDays < 10)
                {
                    throw new BadRequestException($"Quy tắc tạo trại: Ngày đóng đăng ký phải trước ngày bắt đầu ít nhất 10 ngày. (Hiện tại: {buffer.TotalDays:F1} ngày)");
                }
            }
            else // update or extend registration
            {
                if (buffer.TotalDays < 5)
                {
                    throw new BadRequestException($"Quy tắc cập nhật/gia hạn: Thời gian đóng đăng ký phải trước ngày bắt đầu trại ít nhất 5 ngày để đảm bảo công tác chuẩn bị.");
                }
            }
        }

        private async Task RunValidationChecks(CampRequestDto campRequest, int? currentCampId = null)
        {
            // check nullables
            if (!campRequest.StartDate.HasValue || !campRequest.EndDate.HasValue ||
                !campRequest.RegistrationStartDate.HasValue || !campRequest.RegistrationEndDate.HasValue ||
                !campRequest.LocationId.HasValue || !campRequest.CampTypeId.HasValue ||
                !campRequest.MinParticipants.HasValue || !campRequest.MaxParticipants.HasValue ||
                !campRequest.MinAge.HasValue || !campRequest.MaxAge.HasValue)
            {
                throw new BadRequestException("Ngày bắt đầu/kết thúc, ngày đăng ký, địa điểm, loại trại, số lượng tham gia và độ tuổi là các trường bắt buộc.");
            }

            // validation
            if (string.IsNullOrWhiteSpace(campRequest.Name) ||
                string.IsNullOrWhiteSpace(campRequest.Description) ||
                string.IsNullOrWhiteSpace(campRequest.Place) ||
                string.IsNullOrWhiteSpace(campRequest.Address))
            {
                throw new BadRequestException("Tên, mô tả, địa điểm tổ chức và địa chỉ không được để trống.");
            }

            var reqStartDate = campRequest.StartDate.Value;
            var reqEndDate = campRequest.EndDate.Value;
            var reqRegEndDate = campRequest.RegistrationEndDate.Value; // DateTime

            // registration date
            if (campRequest.RegistrationStartDate.Value >= reqRegEndDate)
            {
                throw new BadRequestException("Ngày đóng đăng ký phải sau ngày mở đăng ký.");
            }

            if (reqRegEndDate.Date >= reqStartDate.Date)
            {
                throw new BadRequestException("Ngày đóng đăng ký phải trước ngày bắt đầu trại.");
            }

            if (reqStartDate >= reqEndDate)
            {
                throw new BadRequestException("Ngày kết thúc trại phải sau ngày bắt đầu trại.");
            }

            // check buffer time 
            bool isCreateNew = (currentCampId == null);
            ValidateRegistrationBufferTime(reqStartDate, reqRegEndDate, isCreateNew);

            TimeSpan duration = reqEndDate.Date - reqStartDate.Date;
            if (duration.TotalDays < 3)
            {
                throw new BadRequestException("Thời lượng trại phải kéo dài ít nhất 3 ngày.");
            }

            if (campRequest.MinParticipants.Value <= 0 || campRequest.MaxParticipants.Value <= 0)
            {
                throw new BadRequestException("Số lượng tham gia tối thiểu và tối đa phải lớn hơn 0.");
            }
            if (campRequest.MinParticipants.Value > campRequest.MaxParticipants.Value)
            {
                throw new BadRequestException("Số lượng tham gia tối thiểu không được lớn hơn số lượng tham gia tối đa.");
            }

            if (campRequest.MinAge.Value < 0 || campRequest.MaxAge.Value < 0)
            {
                throw new BadRequestException("Giới hạn độ tuổi không được là số âm.");
            }
            if (campRequest.MinAge.Value > campRequest.MaxAge.Value)
            {
                throw new BadRequestException("Độ tuổi tối thiểu không được lớn hơn độ tuổi tối đa.");
            }

            if (campRequest.Price.HasValue && campRequest.Price.Value < 0)
            {
                throw new BadRequestException("Giá trại không được là số âm.");
            }

            // check same location
            var newCampStartDate = reqStartDate;
            var newCampEndDate = reqEndDate;
            var locationId = campRequest.LocationId.Value;

            DateTime newStartDateTime = newCampStartDate.Date;
            DateTime newEndDateTime = newCampEndDate.Date.AddDays(1).AddTicks(-1);

            var overlappingCamps = await _unitOfWork.Camps.GetQueryable()
                .Where(c => c.locationId == locationId &&
                             c.campId != currentCampId &&
                             (c.status != CampStatus.Canceled.ToString()) &&
                             c.startDate.HasValue && c.endDate.HasValue &&
                             // check duplicate (StartA <= EndB) AND (EndA >= StartB)
                             (c.startDate.Value <= newEndDateTime) &&
                             (c.endDate.Value >= newStartDateTime))
                .ToListAsync();
            if (overlappingCamps.Any())
            {
                throw new BadRequestException($"Địa điểm này đã có Camp ({overlappingCamps.First().name}) hoạt động trong khoảng thời gian từ {overlappingCamps.First().startDate.Value.ToShortDateString()} đến {overlappingCamps.First().endDate.Value.ToShortDateString()}.");
            }
        }

        private async Task ApproveCampAndSchedulesAsync(int campId)
        {

            string approvedStatus = TransportScheduleStatus.NotYet.ToString();

            // APPROVE ALL ACTIVITY SCHEDULES
            var activitySchedules = await _unitOfWork.ActivitySchedules.GetQueryable()
                                        .Include(s => s.activity)
                                        .Where(s => s.activity.campId == campId)
                                        .ToListAsync();

            foreach (var schedule in activitySchedules)
            {
                schedule.status = approvedStatus;
                await _unitOfWork.ActivitySchedules.UpdateAsync(schedule);
            }

            // APPROVE ALL TRANSPORT SCHEDULES
            var campRouteIds = await _unitOfWork.Routes.GetQueryable()
                                                     .Where(r => r.campId == campId)
                                                     .Select(r => r.routeId)
                                                     .ToListAsync();

            var transportSchedules = await _unitOfWork.TransportSchedules.GetQueryable()
                                         .Where(s => s.routeId.HasValue && campRouteIds.Contains(s.routeId.Value))
                                         .ToListAsync();

            foreach (var schedule in transportSchedules)
            {
                schedule.status = approvedStatus;
                await _unitOfWork.TransportSchedules.UpdateAsync(schedule);
            }
        }

        private async Task RejectCampAndSchedulesAsync(int campId)
        {
            string rejectedStatus = TransportScheduleStatus.Rejected.ToString();
            string draftStatus = TransportScheduleStatus.Draft.ToString();

            // REJECT ALL ACTIVITY SCHEDULES WITH STATUS = 'Draft'
            var activitySchedules = await _unitOfWork.ActivitySchedules.GetQueryable()
                                        .Include(s => s.activity)
                                        .Where(s => s.activity.campId == campId && s.status == draftStatus)
                                        .ToListAsync();

            foreach (var schedule in activitySchedules)
            {
                schedule.status = rejectedStatus;
                await _unitOfWork.ActivitySchedules.UpdateAsync(schedule);
            }

            // REJECT ALL TRANSPORT SCHEDULES WITH STATUS = 'Draft'
            var campRouteIds = await _unitOfWork.Routes.GetQueryable()
                                                     .Where(r => r.campId == campId)
                                                     .Select(r => r.routeId)
                                                     .ToListAsync();

            var transportSchedules = await _unitOfWork.TransportSchedules.GetQueryable()
                                         .Where(s => s.routeId.HasValue &&
                                                     campRouteIds.Contains(s.routeId.Value) &&
                                                     s.status == draftStatus)
                                         .ToListAsync();

            foreach (var schedule in transportSchedules)
            {
                schedule.status = rejectedStatus;
                await _unitOfWork.TransportSchedules.UpdateAsync(schedule);
            }
        }

        private async Task ResetRejectedSchedulesToDraftAsync(int campId)
        {
            string rejectedStatus = TransportScheduleStatus.Rejected.ToString();
            string draftStatus = TransportScheduleStatus.Draft.ToString();

            // rejected -> draft
            var activitySchedules = await _unitOfWork.ActivitySchedules.GetQueryable()
                                        .Include(s => s.activity)
                                        .Where(s => s.activity.campId == campId && s.status == rejectedStatus)
                                        .ToListAsync();

            foreach (var schedule in activitySchedules)
            {
                schedule.status = draftStatus;
                await _unitOfWork.ActivitySchedules.UpdateAsync(schedule);
            }

            // rejected -> draft
            var campRouteIds = await _unitOfWork.Routes.GetQueryable()
                                                     .Where(r => r.campId == campId)
                                                     .Select(r => r.routeId)
                                                     .ToListAsync();

            var transportSchedules = await _unitOfWork.TransportSchedules.GetQueryable()
                                         .Where(s => s.routeId.HasValue && campRouteIds.Contains(s.routeId.Value) && s.status == rejectedStatus)
                                         .ToListAsync();

            foreach (var schedule in transportSchedules)
            {
                schedule.status = draftStatus;
                await _unitOfWork.TransportSchedules.UpdateAsync(schedule);
            }
        }

        private async Task CancelCampAndRelatedEntitiesAsync(int campId)
        {
            string canceledStatus = RegistrationStatus.Canceled.ToString();
            string canceledScheduleStatus = ActivityScheduleStatus.Canceled.ToString();
            string canceledTransportStatus = TransportScheduleStatus.Canceled.ToString();
            string canceledCamperStatus = RegistrationCamperStatus.Canceled.ToString();

            // cancel all activity schedules
            var activitySchedules = await _unitOfWork.ActivitySchedules.GetQueryable()
                                        .Include(s => s.activity)
                                        .Where(s => s.activity.campId == campId)
                                        .ToListAsync();

            foreach (var schedule in activitySchedules)
            {
                schedule.status = canceledScheduleStatus;
                await _unitOfWork.ActivitySchedules.UpdateAsync(schedule);
            }

            // cancel all transport schedules
            var campRouteIds = await _unitOfWork.Routes.GetQueryable()
                                                     .Where(r => r.campId == campId)
                                                     .Select(r => r.routeId)
                                                     .ToListAsync();

            var transportSchedules = await _unitOfWork.TransportSchedules.GetQueryable()
                                         .Where(s => s.routeId.HasValue && campRouteIds.Contains(s.routeId.Value))
                                         .ToListAsync();

            foreach (var schedule in transportSchedules)
            {
                schedule.status = canceledTransportStatus;
                await _unitOfWork.TransportSchedules.UpdateAsync(schedule);
            }

            // cancel all registrations
            var registrations = await _unitOfWork.Registrations.GetQueryable()
                                    .Where(r => r.campId == campId)
                                    .ToListAsync();

            foreach (var registration in registrations)
            {
                registration.status = canceledStatus;
                await _unitOfWork.Registrations.UpdateAsync(registration);
            }

            // cancel all registration campers
            var registrationCampers = await _unitOfWork.RegistrationCampers.GetQueryable()
                                            .Include(rc => rc.registration)
                                            .Where(rc => rc.registration.campId == campId)
                                            .ToListAsync();

            foreach (var regCamper in registrationCampers)
            {
                regCamper.status = canceledCamperStatus;
                await _unitOfWork.RegistrationCampers.UpdateAsync(regCamper);
            }

            await _unitOfWork.CommitAsync();

            _logger.LogInformation($"Canceled all related entities for Camp {campId}: {activitySchedules.Count} activity schedules, {transportSchedules.Count} transport schedules, {registrations.Count} registrations, {registrationCampers.Count} registration campers.");
        }

        #endregion


        public async Task<CampValidationResponseDto> ValidateCampReadinessAsync(int campId)
        {
            var response = new CampValidationResponseDto { IsValid = true };
            var dbContext = _unitOfWork.GetDbContext();

            // get camp data
            var camp = await _unitOfWork.Camps.GetByIdAsync(campId);
            if (camp == null) throw new KeyNotFoundException("Không tìm thấy trại.");

            if (!camp.startDate.HasValue || !camp.endDate.HasValue)
            {
                response.Errors.Add("Trại chưa thiết lập ngày bắt đầu và kết thúc.");
                response.IsValid = false;
                return response; // stop here if no dates
            }

            // validate groups
            var groups = await _unitOfWork.Groups.GetByCampIdAsync(campId);

            // check staff assignment and activity assignment
            var unassignedGroups = groups.Where(g => g.supervisorId == null).Select(g => $"{g.groupName} (ID: {g.groupId})").ToList();
            if (unassignedGroups.Any())
            {
                response.Errors.Add($"Các nhóm sau chưa có Staff phụ trách: {string.Join(", ", unassignedGroups)}.");
            }

            var assignedGroupIds = await _unitOfWork.Groups.GetGroupIdsWithSchedulesAsync(campId);

            // find groups without any scheduled activities
            var groupsWithoutSchedule = groups
                .Where(g => !assignedGroupIds.Contains(g.groupId))
                .Select(g => $"{g.groupName} (ID: {g.groupId})")
                .ToList();

            if (groupsWithoutSchedule.Any())
            {
                response.Errors.Add($"Các nhóm sau chưa được phân công bất kỳ hoạt động nào: {string.Join(", ", groupsWithoutSchedule)}.");
            }

            // check total capacity of groups vs camp maxParticipants
            int totalGroupCap = groups.Sum(g => g.maxSize ?? 0);

            // group capacity > camp maxParticipants
            if (totalGroupCap < camp.maxParticipants)
            {
                response.Errors.Add($"Tổng sức chứa của các nhóm ({totalGroupCap}) thấp hơn sức chứa yêu cầu của trại ({camp.minParticipants}).");
            }

            // validate accommodations
            var accommodations = await _unitOfWork.Accommodations.GetByCampIdAsync(campId);

            // check staff assignment
            var unassignedAccs = accommodations.Where(a => a.supervisorId == null).Select(a => $"{a.name} (ID: {a.accommodationId})").ToList();
            if (unassignedAccs.Any())
            {
                response.Errors.Add($"Các khu chỗ ở sau chưa có Staff quản lý: {string.Join(", ", unassignedAccs)}.");
            }

            var assignedAccIds = await _unitOfWork.Accommodations.GetAccommodationIdsWithSchedulesAsync(campId);

            // find accommodations without any scheduled resting periods
            var accsWithoutSchedule = accommodations
                .Where(a => !assignedAccIds.Contains(a.accommodationId))
                .Select(a => $"{a.name} (ID: {a.accommodationId})")
                .ToList();

            if (accsWithoutSchedule.Any())
            {
                response.Errors.Add($"Các khu chỗ ở sau chưa được xếp lịch (Resting): {string.Join(", ", accsWithoutSchedule)}.");
            }

            // check total capacity of accommodations vs camp maxParticipants
            int totalAccCap = accommodations.Sum(a => a.capacity ?? 0);

            if (totalAccCap < camp.maxParticipants)
            {
                response.Errors.Add($"Tổng sức chứa chỗ ở ({totalAccCap}) thấp hơn sức chứa yêu cầu của trại ({camp.minParticipants}).");
            }

            // validate activity schedules
            var schedules = await _unitOfWork.ActivitySchedules.GetScheduleByCampIdAsync(campId);

            if (!schedules.Any())
            {
                response.Errors.Add("Trại chưa có bất kỳ lịch trình nào.");
                response.IsValid = false;
                return response;
            }

            // check initial and final activities
            var firstActivity = schedules.First();
            var lastActivity = schedules.Last();

            if (firstActivity.activity.activityType != ActivityType.Checkin.ToString())
            {
                response.Errors.Add($"Hoạt động đầu tiên phải là 'CheckIn'. Hiện tại đang là: '{firstActivity.activity.name}' ({firstActivity.activity.activityType}).");
            }

            if (lastActivity.activity.activityType != ActivityType.Checkout.ToString())
            {
                response.Errors.Add($"Hoạt động cuối cùng phải là 'CheckOut'. Hiện tại đang là: '{lastActivity.activity.name}' ({lastActivity.activity.activityType}).");
            }

            // check daily coverage
            // for each date in camp duration check if any schedule covers that date
            int daysCount = 0;

            // only consider schedules with both startTime and endTime
            var validSchedules = schedules
                .Where(s => s.startTime.HasValue && s.endTime.HasValue)
                .ToList();

            var loopEndDate = camp.endDate.Value.Date;

            // if end time is 00:00:00 (midnight), 
            // that means the last day is not included in the camp duration
            // subtract one day from the loop end date
            if (camp.endDate.Value.TimeOfDay == TimeSpan.Zero)
            {
                loopEndDate = loopEndDate.AddDays(-1);
            }

            for (var date = camp.startDate.Value.Date; date <= loopEndDate; date = date.AddDays(1))
            {
                // check if any schedule covers this date
                bool hasActivity = validSchedules.Any(s => s.startTime.Value.Date <= date && s.endTime.Value.Date >= date);

                if (!hasActivity)
                {
                    response.Errors.Add($"Ngày {date:dd/MM/yyyy} chưa có bất kỳ hoạt động nào.");
                }
                else
                {
                    daysCount++;
                }
            }

            if (response.Errors.Any())
            {
                response.IsValid = false;
            }

            return response;
        }

    }
}
