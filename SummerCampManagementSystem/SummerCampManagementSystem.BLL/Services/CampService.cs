using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SummerCampManagementSystem.BLL.DTOs.Camp;
using SummerCampManagementSystem.BLL.Helpers;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.BLL.Jobs;
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

            // Schedule Hangfire job for attendance folder creation when registration ends
            if (newCamp.registrationEndDate.HasValue)
            {
                var jobId = AttendanceFolderCreationJob.ScheduleForCamp(
                    newCamp.campId,
                    newCamp.registrationEndDate.Value);
                _logger.LogInformation("Scheduled attendance folder creation job {JobId} for Camp {CampId} at {RegistrationEndDate}",
                    jobId, newCamp.campId, newCamp.registrationEndDate.Value);
            }

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
                ?? throw new KeyNotFoundException($"Staff with ID {staffId} not found.");

            var camps = await _unitOfWork.Camps.GetCampsByStaffIdAsync(staffId)
                ?? throw new KeyNotFoundException($"No camps found for staff ID {staffId}.");

            return _mapper.Map<IEnumerable<CampResponseDto>>(camps);
        }

        public async Task<CampResponseDto?> GetCampByIdAsync(int id)
        {
            var camp = await GetCampsWithIncludes()
                .FirstOrDefaultAsync(c => c.campId == id);

            return _mapper.Map<CampResponseDto>(camp);
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
                throw new KeyNotFoundException($"Camp with ID {campId} not found.");
            }

            if (!Enum.TryParse(existingCamp.status, true, out CampStatus currentStatus))
            {
                throw new InvalidOperationException("Trạng thái hiện tại của Camp không hợp lệ.");
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
                throw new ArgumentException($"Chuyển đổi trạng thái từ '{currentStatus}' sang '{newStatus}' không hợp lệ theo flow đã quy định.");
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
            var existingCamp = await GetCampsWithIncludes()
                .FirstOrDefaultAsync(c => c.campId == campId);

            if (existingCamp == null)
            {
                throw new KeyNotFoundException("Camp not found");
            }

            // only allow update when status is Draft or Rejected
            if (existingCamp.status != CampStatus.Draft.ToString() && existingCamp.status != CampStatus.Rejected.ToString())
            {
                // Bổ sung: Cho phép chỉnh sửa khi UnderEnrolled nếu Admin muốn thay đổi MinCapacity/thời gian trước khi gia hạn? 
                // Nếu không, chỉ giữ Draft/Rejected.
                throw new ArgumentException($"Camp chỉ có thể được chỉnh sửa nội dung khi ở trạng thái Draft hoặc Rejected. Trạng thái hiện tại: {existingCamp.status}.");
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

            await _unitOfWork.Camps.UpdateAsync(existingCamp);
            await _unitOfWork.CommitAsync();

            // Reschedule Hangfire job if registration end date changed
            if (existingCamp.registrationEndDate.HasValue)
            {
                var jobId = AttendanceFolderCreationJob.ScheduleForCamp(
                    existingCamp.campId,
                    existingCamp.registrationEndDate.Value);
                _logger.LogInformation("Rescheduled attendance folder creation job {JobId} for Camp {CampId} at {RegistrationEndDate}",
                    jobId, existingCamp.campId, existingCamp.registrationEndDate.Value);
            }

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

        // change status Draft to PendingApproval and check required conditions
        public async Task<CampResponseDto> SubmitForApprovalAsync(int campId)
        {
            var existingCamp = await GetCampsWithIncludes()
                .FirstOrDefaultAsync(c => c.campId == campId);

            if (existingCamp == null)
            {
                throw new Exception($"Camp with ID {campId} not found.");
            }

            if (!Enum.TryParse(existingCamp.status, true, out CampStatus currentStatus) ||
                (currentStatus != CampStatus.Draft && currentStatus != CampStatus.Rejected))
            {
                throw new ArgumentException($"Camp hiện tại đang ở trạng thái '{currentStatus}'. Chỉ có thể gửi phê duyệt từ trạng thái Draft.");
            }


            // CampStaffAssignments check Group/Staff. 
            // Activities check activity.

            // This section checks Activity and Group/Staff Assignment.

            var hasActivities = await _unitOfWork.Activities.GetQueryable()
                .AnyAsync(a => a.campId == campId);

            var hasGroupsOrStaff = await _unitOfWork.Camps.GetQueryable()
                .Where(c => c.campId == campId)
                .Select(c => c.CampStaffAssignments.Any() || c.CamperGroups.Any()) // check staff or group
                .FirstOrDefaultAsync();

            if (!hasActivities)
            {
                throw new ArgumentException("Không thể gửi phê duyệt. Camp cần có ít nhất một hoạt động (Activity) được tạo.");
            }
            if (!hasGroupsOrStaff)
            {
                throw new ArgumentException("Không thể gửi phê duyệt. Camp cần có ít nhất một Group/Staff Assignment hoặc Camper Group được tạo.");
            }

            return await TransitionCampStatusAsync(campId, CampStatus.PendingApproval);
        }

        #region Scheduled Status Transitions

        public async Task RunScheduledStatusTransitionsAsync()
        {
            // take camp with status Published, OpenForRegistration, RegistrationClosed, InProgress
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
                    // make sure the status is valid enum
                    if (!Enum.TryParse(camp.status, true, out CampStatus currentStatus)) continue;

                    CampStatus? nextStatus = null;
                    _logger.LogDebug($"Checking Camp ID {camp.campId} ('{camp.name}') - Current Status: {currentStatus}.");

                    // LOGIC TRANSITION BASED ON TIME AND CONDITIONS

                    // pubished -> OpenForRegistration 
                    if (currentStatus == CampStatus.Published &&
                        camp.registrationStartDate.HasValue &&
                        camp.registrationStartDate.Value <= utcNow)
                    {
                        nextStatus = CampStatus.OpenForRegistration;
                    }

                    // openForRegistration -> RegistrationClosed 
                    else if (currentStatus == CampStatus.OpenForRegistration &&
                             camp.registrationEndDate.HasValue &&
                             camp.registrationEndDate.Value <= utcNow)
                    {

                        /*
                         * need to check if enough campers registered
                         * missing underEnrolled check here
                         */


                        nextStatus = CampStatus.RegistrationClosed;
                    }

                    // registrationClosed -> InProgress
                    else if (currentStatus == CampStatus.RegistrationClosed &&
                             camp.startDate.HasValue &&
                             camp.startDate.Value <= utcNow)
                    {
                        nextStatus = CampStatus.InProgress;
                    }

                    // inProgress -> Completed
                    else if (currentStatus == CampStatus.InProgress &&
                             camp.endDate.HasValue &&
                             camp.endDate.Value <= utcNow)
                    {
                        nextStatus = CampStatus.Completed;
                    }

                    if (nextStatus.HasValue)
                    {
                        _logger.LogInformation($"Transitioning Camp ID {camp.campId} from {currentStatus} to {nextStatus.Value}...");

                        // use the existing method to transition status
                        await TransitionCampStatusAsync(camp.campId, nextStatus.Value);

                        _logger.LogInformation($"Successfully transitioned Camp ID {camp.campId} to {nextStatus.Value}.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"FATAL ERROR processing Camp ID {camp.campId}. Skipping this camp.");
                }
            }
            _logger.LogInformation("--- FINISHED scheduled camp status transition job. ---");
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

        private async Task RunValidationChecks(CampRequestDto campRequest, int? currentCampId = null)
        {
            // check nullables
            if (!campRequest.StartDate.HasValue || !campRequest.EndDate.HasValue ||
                !campRequest.RegistrationStartDate.HasValue || !campRequest.RegistrationEndDate.HasValue ||
                !campRequest.LocationId.HasValue || !campRequest.CampTypeId.HasValue ||
                !campRequest.MinParticipants.HasValue || !campRequest.MaxParticipants.HasValue ||
                !campRequest.MinAge.HasValue || !campRequest.MaxAge.HasValue)
            {
                throw new ArgumentException("Ngày bắt đầu/kết thúc, ngày đăng ký, địa điểm, loại trại, số lượng tham gia và độ tuổi là các trường bắt buộc.");
            }

            // validation
            if (string.IsNullOrWhiteSpace(campRequest.Name) ||
                string.IsNullOrWhiteSpace(campRequest.Description) ||
                string.IsNullOrWhiteSpace(campRequest.Place) ||
                string.IsNullOrWhiteSpace(campRequest.Address))
            {
                throw new ArgumentException("Tên, mô tả, địa điểm tổ chức và địa chỉ không được để trống.");
            }

            var reqStartDate = campRequest.StartDate.Value;
            var reqEndDate = campRequest.EndDate.Value;
            var reqRegEndDate = campRequest.RegistrationEndDate.Value; // DateTime

            // registration date
            if (campRequest.RegistrationStartDate.Value >= reqRegEndDate)
            {
                throw new ArgumentException("Ngày đóng đăng ký phải sau ngày mở đăng ký.");
            }

            if (reqRegEndDate.Date >= reqStartDate.Date)
            {
                throw new ArgumentException("Ngày đóng đăng ký phải trước ngày bắt đầu trại.");
            }

            if (reqStartDate >= reqEndDate)
            {
                throw new ArgumentException("Ngày kết thúc trại phải sau ngày bắt đầu trại.");
            }

            TimeSpan duration = reqEndDate.Date - reqStartDate.Date;
            if (duration.TotalDays < 3)
            {
                throw new ArgumentException("Thời lượng trại phải kéo dài ít nhất 3 ngày.");
            }

            if (campRequest.MinParticipants.Value <= 0 || campRequest.MaxParticipants.Value <= 0)
            {
                throw new ArgumentException("Số lượng tham gia tối thiểu và tối đa phải lớn hơn 0.");
            }
            if (campRequest.MinParticipants.Value > campRequest.MaxParticipants.Value)
            {
                throw new ArgumentException("Số lượng tham gia tối thiểu không được lớn hơn số lượng tham gia tối đa.");
            }

            if (campRequest.MinAge.Value < 0 || campRequest.MaxAge.Value < 0)
            {
                throw new ArgumentException("Giới hạn độ tuổi không được là số âm.");
            }
            if (campRequest.MinAge.Value > campRequest.MaxAge.Value)
            {
                throw new ArgumentException("Độ tuổi tối thiểu không được lớn hơn độ tuổi tối đa.");
            }

            if (campRequest.Price.HasValue && campRequest.Price.Value < 0)
            {
                throw new ArgumentException("Giá trại không được là số âm.");
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
                throw new ArgumentException($"Địa điểm này đã có Camp ({overlappingCamps.First().name}) hoạt động trong khoảng thời gian từ {overlappingCamps.First().startDate.Value.ToShortDateString()} đến {overlappingCamps.First().endDate.Value.ToShortDateString()}.");
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

        #endregion
    }
}
