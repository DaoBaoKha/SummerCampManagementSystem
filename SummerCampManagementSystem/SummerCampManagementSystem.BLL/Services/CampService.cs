using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.BLL.DTOs.Camp;
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

        public CampService(IUnitOfWork unitOfWork, IMapper mapper, IUserContextService userContextService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _userContextService = userContextService;
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

        public async Task<CampResponseDto?> GetCampByIdAsync(int id)
        {
            var camp = await GetCampsWithIncludes()
                .FirstOrDefaultAsync(c => c.campId == id);

            return camp == null ? null : _mapper.Map<CampResponseDto>(camp);
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
                throw new Exception($"Camp with ID {campId} not found.");
            }

            if (!Enum.TryParse(existingCamp.status, true, out CampStatus currentStatus))
            {
                throw new Exception("Trạng thái hiện tại của Camp không hợp lệ.");
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
                throw new Exception("Camp not found");
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


            if (existingCamp.status == CampStatus.Rejected.ToString())
            {
                existingCamp.status = CampStatus.Draft.ToString(); // back to draft after updating
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

        // change status Draft to PendingApproval and check required conditions
        public async Task<CampResponseDto> SubmitForApprovalAsync(int campId)
        {
            var existingCamp = await GetCampsWithIncludes()
                .FirstOrDefaultAsync(c => c.campId == campId);

            if (existingCamp == null)
            {
                throw new Exception($"Camp with ID {campId} not found.");
            }

            if (!Enum.TryParse(existingCamp.status, true, out CampStatus currentStatus) || currentStatus != CampStatus.Draft)
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

        // help method to include related entities
        private IQueryable<Camp> GetCampsWithIncludes()
        {
            //load related entities
            return _unitOfWork.Camps.GetQueryable()
                .Include(c => c.campType)
                .Include(c => c.location)
                .Include(c => c.promotion);
        }


        /// <param name="campRequest">Dữ liệu Camp Request.</param>
        /// <param name="currentCampId">ID của Camp (để loại trừ chính nó khi check trùng lặp).</param>
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

            if (campRequest.RegistrationStartDate.Value >= campRequest.RegistrationEndDate.Value)
            {
                throw new ArgumentException("Ngày đóng đăng ký phải sau ngày mở đăng ký.");
            }

            if (campRequest.RegistrationEndDate.Value.Date >= campRequest.StartDate.Value.ToDateTime(TimeOnly.MinValue).Date)
            {
                throw new ArgumentException("Ngày đóng đăng ký phải trước ngày bắt đầu trại.");
            }

            if (campRequest.StartDate.Value >= campRequest.EndDate.Value)
            {
                throw new ArgumentException("Ngày kết thúc trại phải sau ngày bắt đầu trại.");
            }

            if (campRequest.EndDate.Value.DayNumber - campRequest.StartDate.Value.DayNumber < 3)
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
            var newCampStartDate = campRequest.StartDate.Value;
            var newCampEndDate = campRequest.EndDate.Value;
            var locationId = campRequest.LocationId.Value;

            // make sure to change to datetime
            DateTime newStartDateTime = newCampStartDate.ToDateTime(TimeOnly.MinValue);
            DateTime newEndDateTime = newCampEndDate.ToDateTime(TimeOnly.MaxValue);

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
    }
}
