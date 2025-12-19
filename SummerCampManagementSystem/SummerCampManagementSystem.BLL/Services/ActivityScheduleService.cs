using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.BLL.DTOs.Activity;
using SummerCampManagementSystem.BLL.DTOs.ActivitySchedule;
using SummerCampManagementSystem.BLL.DTOs.Group;
using SummerCampManagementSystem.BLL.Exceptions;
using SummerCampManagementSystem.BLL.Helpers;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.Core.Enums;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;
using System.Diagnostics;

namespace SummerCampManagementSystem.BLL.Services
{
    public class ActivityScheduleService : IActivityScheduleService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ActivityScheduleService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ActivityScheduleResponseDto>> GetAllSchedulesAsync()
        {
            var activities = await _unitOfWork.ActivitySchedules.GetAllSchedule();
            return _mapper.Map<IEnumerable<ActivityScheduleResponseDto>>(activities);
        }

        public async Task<ActivityScheduleResponseDto?> GetScheduleByIdAsync(int id)
        {
            var schedule = await _unitOfWork.ActivitySchedules.GetScheduleById(id)
                ?? throw new KeyNotFoundException("Activity schedule not found.");
            return schedule == null ? null : _mapper.Map<ActivityScheduleResponseDto>(schedule);
        }

        public async Task<object> GetAllSchedulesByStaffIdAsync(int staffId, int campId)
        {
            var camp = await _unitOfWork.Camps.GetByIdAsync(campId)
                ?? throw new KeyNotFoundException("Camp not found.");

            var schedules = await _unitOfWork.ActivitySchedules.GetAllSchedulesByStaffIdAsync(staffId, campId);

            return new
            {
                camp.campId,
                campName = camp.name,
                activities = schedules.Select(a => new
                {
                    a.activityScheduleId,
                    activityName = a.activity.name,
                    a.activity.activityType,
                    a.startTime,
                    a.endTime,
                    a.status,
                    a.isLivestream,
                    location = a.location.name
                }).ToList()
            };
        }

        public async Task<IEnumerable<ActivityScheduleResponseDto>> GenerateCoreSchedulesFromTemplateAsync(ActivityScheduleTemplateDto templateDto)
        {
            // 1. Validation Tổng quan
            var activity = await _unitOfWork.Activities.GetByIdAsync(templateDto.ActivityId)
                ?? throw new KeyNotFoundException("Activity not found.");

            var camp = await _unitOfWork.Camps.GetByIdAsync(activity.campId.Value)
                ?? throw new KeyNotFoundException("Camp not found.");

            if (templateDto.StartTime >= templateDto.EndTime)
                throw new InvalidOperationException("Giờ bắt đầu phải sớm hơn giờ kết thúc.");

            if (!templateDto.IsDaily && (templateDto.RepeatDays == null || !templateDto.RepeatDays.Any()))
            {
                throw new InvalidOperationException("Phải chọn IsDaily là true hoặc cung cấp danh sách ngày lặp lại.");
            }

            var campStartDate = DateOnly.FromDateTime(camp.startDate.Value);
            var campEndDate = DateOnly.FromDateTime(camp.endDate.Value);

            // 2. Tạo danh sách các ngày cụ thể để tạo lịch trình
            var schedulesToCreate = new List<ActivityScheduleCreateDto>();
            var currentDate = campStartDate;

            while (currentDate <= campEndDate)
            {
                var currentDayOfWeek = (RepeatDayOfWeek)currentDate.DayOfWeek;

                bool shouldCreateSchedule = templateDto.IsDaily;
                if (!shouldCreateSchedule && templateDto.RepeatDays.Any())
                {
                    // Fix: Sử dụng ép kiểu trực tiếp do đã sửa Enum Sunday = 0
                    shouldCreateSchedule = templateDto.RepeatDays.Contains(currentDayOfWeek);
                }

                if (shouldCreateSchedule)
                {
                    var scheduledStartTime = currentDate.ToDateTime(templateDto.StartTime); // VNT
                    var scheduledEndTime = currentDate.ToDateTime(templateDto.EndTime);     // VNT

                    var scheduledStartTimeUtc = scheduledStartTime.ToUtcForStorage();
                    var scheduledEndTimeUtc = scheduledEndTime.ToUtcForStorage();

                    // Kiểm tra: Lịch trình phải nằm trong khoảng thời gian của Camp
                    if (scheduledStartTimeUtc >= camp.startDate.Value && scheduledEndTimeUtc <= camp.endDate.Value)
                    {
                        schedulesToCreate.Add(new ActivityScheduleCreateDto
                        {
                            ActivityId = templateDto.ActivityId,
                            StaffId = templateDto.StaffId,
                            LocationId = templateDto.LocationId,
                            StartTime = scheduledStartTime,
                            EndTime = scheduledEndTime,
                            IsLiveStream = templateDto.IsLiveStream,
                            //IsOptional = false // Luôn là Core Activity
                        });
                    }
                }
                currentDate = currentDate.AddDays(1);
            }

            if (!schedulesToCreate.Any())
            {
                throw new InvalidOperationException("Không thể tạo lịch trình nào. Vui lòng kiểm tra lại Ngày Bắt đầu/Kết thúc và Ngày lặp lại.");
            }

            // 3. Thực hiện Batch Creation và Validation (sử dụng lại logic của CreateCoreScheduleAsync)
            var createdResponses = new List<ActivityScheduleResponseDto>();
            using var transaction = await _unitOfWork.BeginTransactionAsync();

            try
            {
                var groups = await _unitOfWork.Groups.GetByCampIdAsync(camp.campId);
                var accommodations = await _unitOfWork.Accommodations.GetByCampIdAsync(camp.campId);
                var currentCapacity = groups.Sum(g => g.CamperGroups?.Count ?? 0);

                // Tạo một list chứa tất cả thời gian của các schedule sắp tạo
                var allNewScheduleTimesUtc = schedulesToCreate.Select(s => new
                {
                    StartTimeUtc = s.StartTime.ToUtcForStorage(), 
                    EndTimeUtc = s.EndTime.ToUtcForStorage(),  
                    s.LocationId,
                    s.StaffId
                }).ToList();

                // Kiểm tra Xung đột trước khi tạo bất kỳ cái nào
                foreach (var newSchedule in schedulesToCreate)
                {
                    // Kiểm tra xung đột với DB hiện tại
                    var startTimeUtc = newSchedule.StartTime.ToUtcForStorage();
                    var endTimeUtc = newSchedule.EndTime.ToUtcForStorage();

                    // Kiểm tra xung đột với DB hiện tại
                    bool overlap = await _unitOfWork.ActivitySchedules.IsTimeOverlapAsync(
                        camp.campId,
                        startTimeUtc,  
                        endTimeUtc);
                    if (overlap)
                        throw new InvalidOperationException($"Lỗi xung đột DB: Lịch trình {newSchedule.StartTime:dd/MM HH:mm} bị trùng với lịch đã có.");

                    // Kiểm tra xung đột giữa các schedules trong Template với nhau (Tự xung đột)
                    var selfConflictCount = allNewScheduleTimesUtc
                        .Count(s => s.StartTimeUtc < endTimeUtc && s.EndTimeUtc > startTimeUtc &&
                                    s.LocationId == newSchedule.LocationId && s.StaffId == newSchedule.StaffId);

                    if (selfConflictCount > 1)
                        throw new InvalidOperationException($"Lỗi tự xung đột: Lịch trình {newSchedule.StartTime:dd/MM HH:mm} bị trùng lặp trong template.");

                    // Kiểm tra xung đột Location và Staff (Logic đầy đủ nên tham chiếu lại CreateCoreScheduleAsync)
                    // (Đơn giản hóa để không làm phức tạp code template này, nhưng trong thực tế nên gọi lại logic validation)
                }

                // Nếu tất cả đều hợp lệ, thực hiện tạo
                foreach (var dto in schedulesToCreate)
                {
                    int? livestreamId = null;
                    if (dto.IsLiveStream == true && dto.StaffId.HasValue)
                    {
                        var livestream = new Livestream { title = $"{activity.name} - {camp.name}", hostId = dto.StaffId };
                        await _unitOfWork.LiveStreams.CreateAsync(livestream);
                        await _unitOfWork.CommitAsync();
                        livestreamId = livestream.livestreamId;
                    }

                    var schedule = _mapper.Map<ActivitySchedule>(dto);
                    schedule.startTime = dto.StartTime.ToUtcForStorage();
                    schedule.endTime = dto.EndTime.ToUtcForStorage();
                    schedule.currentCapacity = currentCapacity;
                    schedule.livestreamId = livestreamId;
                    schedule.activityId = dto.ActivityId;

                    await _unitOfWork.ActivitySchedules.CreateAsync(schedule);
                    await _unitOfWork.CommitAsync();

                    // Link Group/Accommodation
                    if (activity.activityType == ActivityType.Core.ToString() ||
                        activity.activityType == ActivityType.Checkin.ToString() ||
                        activity.activityType == ActivityType.Checkout.ToString())
                    {
                        foreach (var group in groups)
                        {
                            await _unitOfWork.GroupActivities.CreateAsync(new GroupActivity
                            {
                                groupId = group.groupId,
                                activityScheduleId = schedule.activityScheduleId
                            });
                        }
                    }
                    if (activity.activityType == ActivityType.Resting.ToString())
                    {
                        foreach (var accommodation in accommodations)
                        {
                            await _unitOfWork.AccommodationActivities.CreateAsync(new AccommodationActivitySchedule
                            {
                                accommodationId = accommodation.accommodationId,
                                activityScheduleId = schedule.activityScheduleId
                            });
                        }
                    }
                    await _unitOfWork.CommitAsync();

                    var createdScheduleEntity = await _unitOfWork.ActivitySchedules.GetScheduleById(schedule.activityScheduleId);

                    if (createdScheduleEntity == null)
                    {
                        throw new Exception("Lỗi hệ thống: Không thể truy xuất lịch trình đã tạo sau khi lưu.");
                    }

                    createdResponses.Add(_mapper.Map<ActivityScheduleResponseDto>(createdScheduleEntity));
                }

                await transaction.CommitAsync();
                return createdResponses;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }



        public async Task<CreateScheduleBatchResult> CreateCoreScheduleAsync(ActivityScheduleCreateDto dto)
        {
            var result = new CreateScheduleBatchResult();

            // 1. Lấy thông tin Activity và Camp
            var activity = await _unitOfWork.Activities.GetByIdAsync(dto.ActivityId)
                ?? throw new KeyNotFoundException("Activity not found");

            if (activity.activityType != ActivityType.Core.ToString())
                throw new InvalidOperationException("Activity ID cung cấp không phải là loại Core.");

            var camp = await _unitOfWork.Camps.GetByIdAsync(activity.campId.Value)
                ?? throw new KeyNotFoundException("Camp not found");

            // Validate giờ bắt đầu < kết thúc
            if (dto.StartTime >= dto.EndTime)
                throw new InvalidOperationException("Giờ bắt đầu phải sớm hơn giờ kết thúc.");

            // Validate Staff
            if (dto.StaffId.HasValue)
            {
                var staff = await _unitOfWork.Users.GetByIdAsync(dto.StaffId.Value)
                    ?? throw new KeyNotFoundException("Staff not found.");
                if (!string.Equals(staff.role, "Staff", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("User được gán không phải là Staff.");
            }

            // --- LOGIC TÍNH CAPACITY ---
            // Lấy danh sách Group được chọn và đếm số lượng Camper trong đó
            int totalCapacity = 0;
            var dbContext = _unitOfWork.GetDbContext();

            // Query trực tiếp để Include bảng CamperGroups (đếm thành viên)
            if (dto.GroupIds != null && dto.GroupIds.Any())
            {
                var selectedGroups = await dbContext.Groups
                    .Include(g => g.CamperGroups) // Include bảng phụ để đếm
                    .Where(g => dto.GroupIds.Contains(g.groupId))
                    .ToListAsync();

                // Tính tổng capacity = Tổng số camper trong các group này
                // (Giả sử logic đếm tất cả, nếu cần lọc status Active thì thêm điều kiện vào Count)
                totalCapacity = selectedGroups.Sum(g => g.CamperGroups.Count);
            }

            // 2. Chuẩn hóa ngày giờ (VN -> UTC)
            var campStartVn = camp.startDate.Value.ToVietnamTime();
            var campEndVn = camp.endDate.Value.ToVietnamTime();

            var inputStartUtc = dto.StartTime.Kind == DateTimeKind.Utc ? dto.StartTime : DateTime.SpecifyKind(dto.StartTime, DateTimeKind.Utc);
            var inputEndUtc = dto.EndTime.Kind == DateTimeKind.Utc ? dto.EndTime : DateTime.SpecifyKind(dto.EndTime, DateTimeKind.Utc);

            var dtoStartVn = inputStartUtc.ToVietnamTime();
            var dtoEndVn = inputEndUtc.ToVietnamTime();

            var loopStart = dto.IsRepeat ? campStartVn.Date : dtoStartVn.Date;
            var loopEnd = dto.IsRepeat ? campEndVn.Date : dtoStartVn.Date;

            // List các ngày cần chạy
            var datesToProcess = new List<DateTime>();
            for (var date = loopStart; date <= loopEnd; date = date.AddDays(1))
            {
                datesToProcess.Add(date);
            }

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                foreach (var dateVn in datesToProcess)
                {
                    // Tính toán thời gian cho ngày hiện tại (VN)
                    var currentStartVn = dateVn.Add(dtoStartVn.TimeOfDay);
                    var currentEndVn = dateVn.Add(dtoEndVn.TimeOfDay);

                    // Chuyển sang UTC để check DB và lưu
                    var startTimeUtc = currentStartVn.ToUtcForStorage();
                    var endTimeUtc = currentEndVn.ToUtcForStorage();

                    // --- VALIDATE TỪNG NGÀY ---

                    // 1. Check thời gian trại
                    if (startTimeUtc < camp.startDate.Value || endTimeUtc > camp.endDate.Value)
                    {
                        result.Errors.Add($"Ngày {currentStartVn:dd/MM}: Nằm ngoài thời gian trại.");
                        continue; // Bỏ qua ngày này, chạy ngày tiếp theo
                    }

                    if (dto.LocationId.HasValue)
                    {
                        var location = await _unitOfWork.Locations.GetByIdAsync(dto.LocationId.Value)
                                ?? throw new KeyNotFoundException("Location not found");

                        bool locationConflict = await _unitOfWork.ActivitySchedules
                            .ExistsInSameTimeAndLocationAsync(dto.LocationId.Value, startTimeUtc, endTimeUtc);

                        if (locationConflict)
                        {
                            result.Errors.Add($"Ngày {currentStartVn:dd/MM}: Địa điểm đã có hoạt động khác.");
                            continue;
                        }
                    }

                    // 2. Check Conflict (Core đè Core, Cấm đè Optional/Resting)
                    // Lưu ý: Thêm điều kiện !s.isDeleted nếu có
                    bool hasConflict = await dbContext.ActivitySchedules
                        .Include(s => s.activity)
                        .AnyAsync(s =>
                            s.activity.campId == camp.campId &&
                            (s.startTime < endTimeUtc && s.endTime > startTimeUtc) && // Logic Overlap
                            (s.activity.activityType == ActivityType.Optional.ToString() ||
                             s.activity.activityType == ActivityType.Resting.ToString())
                        );

                    if (hasConflict)
                    {
                        result.Errors.Add($"Ngày {currentStartVn:dd/MM}: Bị trùng lịch với hoạt động Optional/Resting.");
                        continue; // Bỏ qua ngày này
                    }

                    // 3. Check Staff Busy
                    if (dto.StaffId.HasValue)
                    {
                        bool staffBusy = await _unitOfWork.ActivitySchedules
                            .IsStaffBusyAsync(dto.StaffId.Value, startTimeUtc, endTimeUtc);

                        if (staffBusy)
                        {
                            result.Errors.Add($"Ngày {currentStartVn:dd/MM}: Staff bận.");
                            continue; // Bỏ qua ngày này
                        }
                    }

                    // --- NẾU KHÔNG CÓ LỖI THÌ TẠO MỚI ---

                    // Tạo Livestream (nếu có)
                    int? livestreamId = null;
                    if (dto.IsLiveStream == true && dto.StaffId.HasValue)
                    {
                        var livestream = new Livestream
                        {
                            title = $"{activity.name} - {camp.name} ({currentStartVn:dd/MM})",
                            hostId = dto.StaffId
                        };
                        await _unitOfWork.LiveStreams.CreateAsync(livestream);
                        await _unitOfWork.CommitAsync();
                        livestreamId = livestream.livestreamId;
                    }

                    // Tạo Schedule
                    var schedule = _mapper.Map<ActivitySchedule>(dto);
                    schedule.startTime = startTimeUtc;
                    schedule.endTime = endTimeUtc;
                    schedule.livestreamId = livestreamId;

                    // Gán Capacity đã tính ở trên
                    schedule.currentCapacity = totalCapacity;

                    await _unitOfWork.ActivitySchedules.CreateAsync(schedule);
                    await _unitOfWork.CommitAsync(); // Save để lấy ID

                    // Gán GroupActivity
                    if (dto.GroupIds != null && dto.GroupIds.Any())
                    {
                        foreach (var groupId in dto.GroupIds)
                        {
                            var groupActivity = new GroupActivity
                            {
                                groupId = groupId,
                                activityScheduleId = schedule.activityScheduleId
                            };
                            await _unitOfWork.GroupActivities.CreateAsync(groupActivity);
                        }
                        await _unitOfWork.CommitAsync();
                    }

                    // Thêm vào danh sách thành công
                    var createdEntity = await _unitOfWork.ActivitySchedules.GetScheduleById(schedule.activityScheduleId);
                    result.Successes.Add(_mapper.Map<ActivityScheduleResponseDto>(createdEntity));
                }

                // Commit transaction cho những item thành công
                await transaction.CommitAsync();

                return result;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<IEnumerable<GroupNameDto>> GetAvailableGroupsForCoreAsync(GetAvailableGroupRequestDto request)
        {
            // 1. Validate request
            var camp = await _unitOfWork.Camps.GetByIdAsync(request.CampId)
                ?? throw new KeyNotFoundException("Camp not found.");

            if (request.StartTime >= request.EndTime)
                throw new InvalidOperationException("Giờ bắt đầu phải sớm hơn giờ kết thúc.");

            var dbContext = _unitOfWork.GetDbContext();

            // 2. CHECK BLOCKER TOÀN TRẠI (Optional & Resting)
            // Nếu khung giờ này dính Optional hoặc Resting -> Toàn bộ trại bận -> Không nhóm nào rảnh.
            bool isCampBlocked = await dbContext.ActivitySchedules
                .Include(s => s.activity)
                .AnyAsync(s =>
                    s.activity.campId == request.CampId &&
                    (s.startTime < request.EndTime && s.endTime > request.StartTime) && // Overlap Check
                    (s.activity.activityType == ActivityType.Optional.ToString() ||
                     s.activity.activityType == ActivityType.Resting.ToString())
                );

            if (isCampBlocked)
            {
                throw new InvalidOperationException("Không có nhóm nào khả dụng trong khung giờ này do trại đang có hoạt động Optional hoặc Resting.");
            }

            // 3. TÌM CÁC GROUP ĐANG BẬN (Tham gia Core khác)
            // "ko 2 core trùng nhau thì chỉ 1 group dc tham gia thôi" -> Group nào đã dính lịch thì loại ra.
            var busyGroupIds = await dbContext.GroupActivities
                .Include(ga => ga.activitySchedule)
                .Where(ga =>
                    ga.activitySchedule.activity.campId == request.CampId &&
                    (ga.activitySchedule.startTime < request.EndTime && ga.activitySchedule.endTime > request.StartTime)
                )
                .Select(ga => ga.groupId)
                .Distinct()
                .ToListAsync();

            // 4. LẤY DANH SÁCH GROUP KHẢ DỤNG
            var availableGroups = await dbContext.Groups
                .Include(g => g.supervisor)
                .Where(g =>
                    g.campId == request.CampId &&
                    !busyGroupIds.Contains(g.groupId)
                )
                .ToListAsync();

            return _mapper.Map<IEnumerable<GroupNameDto>>(availableGroups);
        }

        public async Task<CreateScheduleBatchResult> CreateOptionalScheduleAsync(OptionalScheduleCreateDto dto)
        {
            var result = new CreateScheduleBatchResult();

            // 1. Lấy thông tin Activity và Camp
            var activity = await _unitOfWork.Activities.GetByIdAsync(dto.ActivityId)
                ?? throw new KeyNotFoundException("Activity not found");

            // Validate: Đảm bảo Activity này đúng là loại Optional
            if (activity.activityType != ActivityType.Optional.ToString())
                throw new InvalidOperationException("Activity ID cung cấp không phải là loại Optional.");

            var camp = await _unitOfWork.Camps.GetByIdAsync(activity.campId.Value)
                ?? throw new KeyNotFoundException("Camp not found");

            // Validate giờ
            if (dto.StartTime >= dto.EndTime)
                throw new InvalidOperationException("Giờ bắt đầu phải sớm hơn giờ kết thúc.");

            // Validate Staff (nếu có)
            if (dto.StaffId.HasValue)
            {
                var staff = await _unitOfWork.Users.GetByIdAsync(dto.StaffId.Value)
                    ?? throw new KeyNotFoundException("Staff not found.");
                if (!string.Equals(staff.role, "Staff", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("User được gán không phải là Staff.");
            }

            // 2. Chuẩn hóa thời gian (VN -> UTC Logic)
            var campStartVn = camp.startDate.Value.ToVietnamTime();
            var campEndVn = camp.endDate.Value.ToVietnamTime();

            // Ép kiểu về Unspecified (Giờ VN) để tính toán
            var inputStartUtc = dto.StartTime.Kind == DateTimeKind.Utc ? dto.StartTime : DateTime.SpecifyKind(dto.StartTime, DateTimeKind.Utc);
            var inputEndUtc = dto.EndTime.Kind == DateTimeKind.Utc ? dto.EndTime : DateTime.SpecifyKind(dto.EndTime, DateTimeKind.Utc);

            var dtoStartVn = inputStartUtc.ToVietnamTime();
            var dtoEndVn = inputEndUtc.ToVietnamTime();

            var loopStart = dto.IsRepeat ? campStartVn.Date : dtoStartVn.Date;
            var loopEnd = dto.IsRepeat ? campEndVn.Date : dtoStartVn.Date;

            var datesToProcess = new List<DateTime>();
            for (var date = loopStart; date <= loopEnd; date = date.AddDays(1))
            {
                datesToProcess.Add(date);
            }

            var dbContext = _unitOfWork.GetDbContext();

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                foreach (var dateVn in datesToProcess)
                {
                    // Tính thời điểm bắt đầu/kết thúc cho ngày hiện tại (Giờ VN)
                    var currentStartVn = dateVn.Add(dtoStartVn.TimeOfDay);
                    var currentEndVn = dateVn.Add(dtoEndVn.TimeOfDay);

                    // Chuyển sang UTC để query DB và Lưu
                    var startTimeUtc = currentStartVn.ToUtcForStorage();
                    var endTimeUtc = currentEndVn.ToUtcForStorage();

                    // === BẮT ĐẦU VALIDATE TỪNG NGÀY ===

                    // 1. Check thời gian nằm trong trại
                    if (startTimeUtc < camp.startDate.Value || endTimeUtc > camp.endDate.Value)
                    {
                        result.Errors.Add($"Ngày {currentStartVn:dd/MM}: Nằm ngoài thời gian trại.");
                        continue;
                    }

                    // 2. Check Location (Địa điểm bận)
                    // Logic: Một địa điểm không thể chứa 2 hoạt động cùng lúc (bất kể loại gì)
                    if (dto.LocationId.HasValue)
                    {
                        var location = await _unitOfWork.Locations.GetByIdAsync(dto.LocationId.Value)
                              ?? throw new KeyNotFoundException("Location not found");

                                        bool locationConflict = await _unitOfWork.ActivitySchedules
                                            .ExistsInSameTimeAndLocationAsync(dto.LocationId.Value, startTimeUtc, endTimeUtc);

                        if (locationConflict)
                        {
                            result.Errors.Add($"Ngày {currentStartVn:dd/MM}: Địa điểm đã có hoạt động khác.");
                            continue;
                        }
                    }

                    // 3. Check Logic Nghiệp vụ (RULE CỦA OPTIONAL)
                    // "Không được đè lên Core/Resting, Optional được phép đè lên nhau"
                    bool typeConflict = await dbContext.ActivitySchedules
                        .Include(s => s.activity)
                        .AnyAsync(s =>
                            s.activity.campId == camp.campId &&
                            (s.startTime < endTimeUtc && s.endTime > startTimeUtc) &&
                            (s.activity.activityType == ActivityType.Core.ToString() ||
                             s.activity.activityType == ActivityType.Resting.ToString()) // Chỉ sợ Core hoặc Resting
                        );

                    if (typeConflict)
                    {
                        result.Errors.Add($"Ngày {currentStartVn:dd/MM}: Bị trùng lịch với hoạt động Core hoặc Resting.");
                        continue;
                    }

                    // 4. Check Staff Busy
                    if (dto.StaffId.HasValue)
                    {
                        bool staffBusy = await _unitOfWork.ActivitySchedules
                            .IsStaffBusyAsync(dto.StaffId.Value, startTimeUtc, endTimeUtc);

                        if (staffBusy)
                        {
                            result.Errors.Add($"Ngày {currentStartVn:dd/MM}: Staff đang bận hoạt động khác.");
                            continue;
                        }
                    }

                    // === TẠO MỚI KHI KHÔNG CÓ LỖI ===

                    // Tạo Livestream
                    int? livestreamId = null;
                    if (dto.IsLiveStream == true && dto.StaffId.HasValue)
                    {
                        var livestream = new Livestream
                        {
                            title = $"{activity.name} - {camp.name} ({currentStartVn:dd/MM})",
                            hostId = dto.StaffId
                        };
                        await _unitOfWork.LiveStreams.CreateAsync(livestream);
                        await _unitOfWork.CommitAsync();
                        livestreamId = livestream.livestreamId;
                    }

                    // Tạo Schedule
                    var schedule = _mapper.Map<ActivitySchedule>(dto);
                    schedule.startTime = startTimeUtc;
                    schedule.endTime = endTimeUtc;
                    schedule.livestreamId = livestreamId;

                    // Optional thường là đăng ký tự do, không gán cứng group
                    // Capacity có thể để null hoặc max int tùy logic của bạn
                    schedule.currentCapacity = 0;

                    await _unitOfWork.ActivitySchedules.CreateAsync(schedule);
                    await _unitOfWork.CommitAsync();


                    // Thêm vào list thành công
                    var createdEntity = await _unitOfWork.ActivitySchedules.GetScheduleById(schedule.activityScheduleId);
                    result.Successes.Add(_mapper.Map<ActivityScheduleResponseDto>(createdEntity));
                }

                await transaction.CommitAsync();
                return result;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<CreateScheduleBatchResult> CreateRestingScheduleAsync(RestingScheduleCreateDto dto)
        {
            var result = new CreateScheduleBatchResult();

            // 1. Validate & Lấy thông tin cơ bản
            var activity = await _unitOfWork.Activities.GetByIdAsync(dto.ActivityId)
                ?? throw new KeyNotFoundException("Activity not found");

            // Đảm bảo đúng loại Resting
            if (activity.activityType != ActivityType.Resting.ToString())
                throw new InvalidOperationException("Activity ID cung cấp không phải là loại Resting.");

            var camp = await _unitOfWork.Camps.GetByIdAsync(activity.campId.Value)
                ?? throw new KeyNotFoundException("Camp not found");

            // Validate giờ
            if (dto.StartTime >= dto.EndTime)
                throw new InvalidOperationException("Giờ bắt đầu phải sớm hơn giờ kết thúc.");

           

            // 3. TÍNH CAPACITY & LẤY LIST ACCOMMODATION
            // Logic: Resting áp dụng cho toàn trại -> Tự động lấy tất cả Accommodation trong trại
            var accommodations = await _unitOfWork.Accommodations.GetByCampIdAsync(camp.campId);
            if (accommodations == null || !accommodations.Any())
                throw new InvalidOperationException("Trại chưa có khu lưu trú (Accommodation) nào để tạo lịch nghỉ ngơi.");


            // 4. CHUẨN HÓA THỜI GIAN (Logic Timezone VN -> UTC)
            var campStartVn = camp.startDate.Value.ToVietnamTime();
            var campEndVn = camp.endDate.Value.ToVietnamTime();

            var inputStartUtc = dto.StartTime.Kind == DateTimeKind.Utc ? dto.StartTime : DateTime.SpecifyKind(dto.StartTime, DateTimeKind.Utc);
            var inputEndUtc = dto.EndTime.Kind == DateTimeKind.Utc ? dto.EndTime : DateTime.SpecifyKind(dto.EndTime, DateTimeKind.Utc);

            var dtoStartVn = inputStartUtc.ToVietnamTime();
            var dtoEndVn = inputEndUtc.ToVietnamTime();

            var loopStart = dto.IsRepeat ? campStartVn.Date : dtoStartVn.Date;
            var loopEnd = dto.IsRepeat ? campEndVn.Date : dtoStartVn.Date;

            var datesToProcess = new List<DateTime>();
            for (var date = loopStart; date <= loopEnd; date = date.AddDays(1))
            {
                datesToProcess.Add(date);
            }

            var dbContext = _unitOfWork.GetDbContext();

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                foreach (var dateVn in datesToProcess)
                {
                    // Tính thời điểm bắt đầu/kết thúc cho ngày hiện tại (Giờ VN)
                    var currentStartVn = dateVn.Add(dtoStartVn.TimeOfDay);
                    var currentEndVn = dateVn.Add(dtoEndVn.TimeOfDay);

                    if (dtoEndVn.TimeOfDay < dtoStartVn.TimeOfDay)
                    {
                        currentEndVn = currentEndVn.AddDays(1); // Cộng thêm 1 ngày cho giờ kết thúc
                    }

                    // Chuyển sang UTC để query DB và Lưu
                    var startTimeUtc = currentStartVn.ToUtcForStorage();
                    var endTimeUtc = currentEndVn.ToUtcForStorage();

                    // === BẮT ĐẦU VALIDATE TỪNG NGÀY ===

                    // Rule 1: Check thời gian nằm trong trại
                    if (startTimeUtc < camp.startDate.Value || endTimeUtc > camp.endDate.Value)
                    {
                        result.Errors.Add($"Ngày {currentStartVn:dd/MM}: Nằm ngoài thời gian trại.");
                        continue;
                    }

                    // Rule 2: Check Conflict (QUAN TRỌNG)
                    // Logic: Resting không được đè lên BẤT KỲ hoạt động nào khác (Core, Optional, Resting khác...)
                    bool hasConflict = await dbContext.ActivitySchedules
                        .Include(s => s.activity)
                        .AnyAsync(s =>
                            s.activity.campId == camp.campId &&
                            (s.startTime < endTimeUtc && s.endTime > startTimeUtc) // Logic overlap
                        );

                    if (hasConflict)
                    {
                        result.Errors.Add($"Ngày {currentStartVn:dd/MM}: Đã bị trùng với một hoạt động khác.");
                        continue;
                    }

                    // === TẠO MỚI ===

                    // Map DTO sang Entity
                    var schedule = _mapper.Map<ActivitySchedule>(dto);
                    schedule.startTime = startTimeUtc;
                    schedule.endTime = endTimeUtc;

                    // Set các trường đặc thù của Resting                   

                    await _unitOfWork.ActivitySchedules.CreateAsync(schedule);
                    await _unitOfWork.CommitAsync(); // Save để lấy ID

                    // === TỰ ĐỘNG PHÂN ACCOMMODATION ===
                    foreach (var acc in accommodations)
                    {
                        var accActivity = new AccommodationActivitySchedule
                        {
                            accommodationId = acc.accommodationId,
                            activityScheduleId = schedule.activityScheduleId
                        };
                        await _unitOfWork.AccommodationActivities.CreateAsync(accActivity);
                    }
                    await _unitOfWork.CommitAsync(); // Save các bảng phụ

                    // Thêm vào list thành công
                    var createdEntity = await _unitOfWork.ActivitySchedules.GetScheduleById(schedule.activityScheduleId);
                    result.Successes.Add(_mapper.Map<ActivityScheduleResponseDto>(createdEntity));
                }

                await transaction.CommitAsync();
                return result;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<ActivityScheduleResponseDto> CreateCheckInCheckOutScheduleAsync(CreateCheckInCheckOutRequestDto dto)
        {
            // 1. Lấy thông tin Activity và Camp
            var activity = await _unitOfWork.Activities.GetByIdAsync(dto.ActivityId)
                ?? throw new KeyNotFoundException("Activity not found");

            var camp = await _unitOfWork.Camps.GetByIdAsync(activity.campId.Value)
                ?? throw new KeyNotFoundException("Camp not found");

            // Vì Check-in/Check-out chỉ diễn ra 1 lần duy nhất trong trại

            if (dto.StartTime < camp.startDate.Value || dto.EndTime > camp.endDate.Value)
            {
                throw new InvalidOperationException("Thời gian Check-in/Check-out phải nằm trong thời gian diễn ra trại.");
            }

            if (activity.activityType == ActivityType.Checkin.ToString())
            {
                if(dto.StartTime != camp.startDate)
                {
                    throw new InvalidOperationException("Thời gian bắt đầu Check-in phải trùng với thời gian trại bắt đầu .");
                }
            }
            else if (activity.activityType == ActivityType.Checkout.ToString())
            {
                if (dto.EndTime != camp.endDate)
                {
                    throw new InvalidOperationException("Thời gian kết thúc Check-out phải trùng với thời gian trại kết thúc.");
                }
            }
            else
            {
                throw new InvalidOperationException("Activity này không phải loại CheckIn hoặc CheckOut.");
            }

            bool alreadyExists = await _unitOfWork.GetDbContext()
                  .ActivitySchedules
                  .Include(s => s.activity)
                  .AnyAsync(s => s.activity.activityType == activity.activityType
                              && s.activity.campId == activity.campId);

            if (alreadyExists)
            {
                throw new InvalidOperationException($"Lịch trình cho hoạt động '{activity.activityType}' đã tồn tại rồi, không thể tạo thêm.");
            }

            // 3. Validate Location và Conflict
            var dbContext = _unitOfWork.GetDbContext();

            bool isOverlap = await _unitOfWork.ActivitySchedules
                   .IsTimeOverlapAsync(camp.campId, dto.StartTime, dto.EndTime);

            if (isOverlap)
                throw new InvalidOperationException("Thời gian checkin/checkout bị trùng với hoạt động khác.");

            // Check xem địa điểm có bận không
            bool locationBusy = await _unitOfWork.ActivitySchedules
                    .ExistsInSameTimeAndLocationAsync(dto.LocationId, dto.StartTime, dto.EndTime);

            if (locationBusy)
                throw new InvalidOperationException("Địa điểm này đã có hoạt động khác trong khung giờ bạn chọn.");

            // 4. Lấy tất cả Group Active để gán tự động
            var allGroups = await dbContext.Groups
                .Include(g => g.CamperGroups) // Include để tính capacity nếu cần
                .Where(g => g.campId == camp.campId)
                .ToListAsync();

            int totalCapacity = allGroups.Sum(g => g.CamperGroups?.Count ?? 0);

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                // 5. Tạo Schedule
                var schedule = new ActivitySchedule
                {
                    activityId = dto.ActivityId,
                    locationId = dto.LocationId,
                    startTime = dto.StartTime,
                    endTime = dto.EndTime,
                    status = ActivityScheduleStatus.Draft.ToString(),
                    currentCapacity = totalCapacity,
                };

                await _unitOfWork.ActivitySchedules.CreateAsync(schedule);
                await _unitOfWork.CommitAsync();

                // 6. Gán Group
                if (allGroups.Any())
                {
                    foreach (var group in allGroups)
                    {
                        await _unitOfWork.GroupActivities.CreateAsync(new GroupActivity
                        {
                            groupId = group.groupId,
                            activityScheduleId = schedule.activityScheduleId
                        });
                    }
                    await _unitOfWork.CommitAsync();
                }

                await transaction.CommitAsync();

                // 7. Return Result
                var createdEntity = await _unitOfWork.ActivitySchedules.GetScheduleById(schedule.activityScheduleId);
                return _mapper.Map<ActivityScheduleResponseDto>(createdEntity);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<ActivityScheduleResponseDto> UpdateScheduleAsync(ActivityScheduleUpdateDto dto)
        {
            // 1. LẤY DỮ LIỆU CŨ & CAMP
            var schedule = await _unitOfWork.ActivitySchedules.GetScheduleById(dto.ActivityScheduleId);


            if (schedule == null)
                throw new KeyNotFoundException("Lịch trình không tồn tại.");

            var activityType = schedule.activity.activityType;
            var campId = schedule.activity.campId.Value;

            var camp = await _unitOfWork.Camps.GetByIdAsync(campId);

            // 2. VALIDATE THỜI GIAN (Global Rule)
            if (dto.StartTime >= dto.EndTime)
                throw new InvalidOperationException("Giờ bắt đầu phải sớm hơn giờ kết thúc.");

            // Rule Mới: Tất cả mọi loại (kể cả CheckIn/Out) đều phải nằm TRONG thời gian trại
            if (dto.StartTime < camp.startDate.Value || dto.EndTime > camp.endDate.Value)
            {
                throw new InvalidOperationException("Thời gian hoạt động phải nằm trọn trong thời gian diễn ra trại.");
            }

            var dbContext = _unitOfWork.GetDbContext();

            // 3. XỬ LÝ LOGIC THEO TỪNG LOẠI
            if (activityType == ActivityType.Resting.ToString())
            {
                // === RESTING ===
                // Chỉ update Time. Force Null các cái khác.
                dto.LocationId = null;
                dto.StaffId = null;
                dto.IsLiveStream = false;

                // Rule: Resting không được trùng với BẤT KỲ cái gì khác.
                bool isOverlap = await _unitOfWork.ActivitySchedules
                    .IsTimeOverlapAsync(campId, dto.StartTime, dto.EndTime, dto.ActivityScheduleId);

                if (isOverlap)
                    throw new InvalidOperationException("Thời gian nghỉ ngơi bị trùng với hoạt động khác.");
            }
            else if (activityType == "CheckIn" || activityType == "CheckOut")
            {
                // === CHECKIN / CHECKOUT ===
                // Update Time, Location. Force Null Staff, Livestream.
                dto.StaffId = null;
                dto.IsLiveStream = false;

                bool isOverlap = await _unitOfWork.ActivitySchedules
                    .IsTimeOverlapAsync(campId, dto.StartTime, dto.EndTime, dto.ActivityScheduleId);

                if (isOverlap)
                    throw new InvalidOperationException($"Thời gian {activityType}" +
                        $" bị trùng với hoạt động khác.");

                // Check Location
                if (dto.LocationId.HasValue)
                {
                    bool locationBusy = await _unitOfWork.ActivitySchedules
                        .ExistsInSameTimeAndLocationAsync(dto.LocationId.Value, dto.StartTime, dto.EndTime, dto.ActivityScheduleId);

                    if (locationBusy)
                        throw new InvalidOperationException("Địa điểm này đang bận.");
                }
            }
            else if (activityType == ActivityType.Core.ToString() || activityType == ActivityType.Optional.ToString())
            {
                // === CORE & OPTIONAL ===
                // Update Full: Time, Location, Staff, LiveStream

                // A. Check Xung đột Logic (Type Conflict)
                var blockerTypes = new List<string> { ActivityType.Resting.ToString() };

                // Core sợ Optional, Optional sợ Core
                if (activityType == ActivityType.Core.ToString())
                    blockerTypes.Add(ActivityType.Optional.ToString());
                else
                    blockerTypes.Add(ActivityType.Core.ToString());

                bool hasTypeConflict = await dbContext.ActivitySchedules
                    .Include(s => s.activity)
                    .AnyAsync(s =>
                        s.activityScheduleId != dto.ActivityScheduleId && 
                        s.activity.campId == campId &&
                        (s.startTime < dto.EndTime && s.endTime > dto.StartTime) &&
                        blockerTypes.Contains(s.activity.activityType)
                    );

                if (hasTypeConflict)
                    throw new InvalidOperationException($"Khung giờ này bị trùng với hoạt động {string.Join("/", blockerTypes)}.");

                // B. Check Location (Dùng hàm Repo có sẵn)
                if (dto.LocationId.HasValue)
                {
                    bool locationBusy = await _unitOfWork.ActivitySchedules
                        .ExistsInSameTimeAndLocationAsync(dto.LocationId.Value, dto.StartTime, dto.EndTime, dto.ActivityScheduleId);

                    if (locationBusy) throw new InvalidOperationException("Địa điểm này đang bận.");
                }

                // C. Check Staff (Dùng hàm Repo có sẵn)
                if (dto.StaffId.HasValue)
                {
                    // Validate Role Staff
                    var staffUser = await _unitOfWork.Users.GetByIdAsync(dto.StaffId.Value);
                    if (staffUser == null || staffUser.role != "Staff")
                        throw new InvalidOperationException("User không phải là Staff.");

                    bool staffBusy = await _unitOfWork.ActivitySchedules
                        .IsStaffBusyAsync(dto.StaffId.Value, dto.StartTime, dto.EndTime, dto.ActivityScheduleId);

                    if (staffBusy) throw new InvalidOperationException("Staff này đang bận lịch khác.");
                }
            }

            // 4. SAVE CHANGES
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Update fields
                schedule.startTime = dto.StartTime;
                schedule.endTime = dto.EndTime;
                schedule.locationId = dto.LocationId;
                schedule.staffId = dto.StaffId;

                // Update Livestream Logic
                if (dto.IsLiveStream == true)
                {
                    // Nếu chưa có livestreamId mà có Staff -> Tạo mới
                    if (schedule.livestreamId == null && dto.StaffId.HasValue)
                    {
                        var newLive = new Livestream
                        {
                            title = $"{schedule.activity.name} - {camp.name}",
                            hostId = dto.StaffId
                        };
                        await _unitOfWork.LiveStreams.CreateAsync(newLive);
                        await _unitOfWork.CommitAsync();
                        schedule.isLivestream = dto.IsLiveStream;
                        schedule.livestreamId = newLive.livestreamId;
                    }
                    // Nếu đã có livestreamId -> Giữ nguyên (hoặc update HostId nếu muốn logic đó)
                }
                else // false hoặc null
                {
                    // Nếu tắt livestream -> set null relationship
                    schedule.livestreamId = null;
                    schedule.isLivestream = dto.IsLiveStream;
                }

                await _unitOfWork.ActivitySchedules.UpdateAsync(schedule);
                await _unitOfWork.CommitAsync();

                await transaction.CommitAsync();

                var updatedEntity = await _unitOfWork.ActivitySchedules.GetScheduleById(schedule.activityScheduleId);
                return _mapper.Map<ActivityScheduleResponseDto>(updatedEntity);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<IEnumerable<ActivityScheduleResponseDto>> GetByCampAndStaffAsync(int campId, int staffId)
        {
            var camp = await _unitOfWork.Camps.GetByIdAsync(campId)
                ?? throw new KeyNotFoundException("Camp not found.");

            var staff = await _unitOfWork.Users.GetByIdAsync(staffId)
                ?? throw new KeyNotFoundException("Staff Not found");
            
            var schedules = await _unitOfWork.ActivitySchedules.GetByCampAndStaffAsync(campId, staffId);

            return _mapper.Map<IEnumerable<ActivityScheduleResponseDto>>(schedules);
        }


        public async Task<IEnumerable<ActivityScheduleResponseDto>> GetCheckInCheckoutByCampAndStaffAsync(int campId, int staffId)
        {
            var camp = await _unitOfWork.Camps.GetByIdAsync(campId)
                ?? throw new KeyNotFoundException("Camp not found.");

            var staff = await _unitOfWork.Users.GetByIdAsync(staffId)
                ?? throw new KeyNotFoundException("Staff Not found");

            var schedules = await _unitOfWork.ActivitySchedules.GetCheckInCheckoutByCampAndStaffAsync(campId, staffId);

            return _mapper.Map<IEnumerable<ActivityScheduleResponseDto>>(schedules);
        }


        public async Task<IEnumerable<ActivityScheduleResponseDto>> GetSchedulesByGroupStaffAsync(int campId, int staffId)
        {
            var camp = await _unitOfWork.Camps.GetByIdAsync(campId)
                ?? throw new KeyNotFoundException("Camp not found.");

            var staff = await _unitOfWork.Users.GetByIdAsync(staffId);

            var schedules = await _unitOfWork.ActivitySchedules.GetSchedulesByGroupStaffAsync(campId, staffId);

            return _mapper.Map<IEnumerable<ActivityScheduleResponseDto>>(schedules);
        }

        public async Task<IEnumerable<ActivityScheduleResponseDto>> GetAllTypeSchedulesByStaffAsync(int campId, int staffId)
        {
            var camp = await _unitOfWork.Camps.GetByIdAsync(campId)
                ?? throw new KeyNotFoundException("Camp not found.");

            var staff = await _unitOfWork.Users.GetByIdAsync(staffId);

            var schedules = await _unitOfWork.ActivitySchedules.GetAllTypeSchedulesByStaffAsync(campId, staffId);

            return _mapper.Map<IEnumerable<ActivityScheduleResponseDto>>(schedules);
        }

        public async Task<IEnumerable<ActivityScheduleByCamperResponseDto>> GetCamperSchedulesAsync(int campId, int camperId)
        {
            var camper = await _unitOfWork.Campers.GetByIdAsync(camperId)
                ?? throw new KeyNotFoundException("Camper not found.");

            var camp = await _unitOfWork.Camps.GetByIdAsync(campId)
                ?? throw new KeyNotFoundException("Camp not found.");

            var isCamperInCamp = await _unitOfWork.ActivitySchedules
                .IsCamperofCamp(campId, camperId);

            if (!isCamperInCamp)
                throw new InvalidOperationException("Camper is not enrolled in the camp.");

            var (groupIds, accommodationIds) = await _unitOfWork.ActivitySchedules.GetCamperGroupAndAccommodationIdsAsync(campId, camperId);

            // 3. Gọi Repository để lấy lịch dựa trên context đó
            var schedules = await _unitOfWork.ActivitySchedules
                .GetPersonalSchedulesAsync(campId, camperId, groupIds, accommodationIds);

            return _mapper.Map<IEnumerable<ActivityScheduleByCamperResponseDto>>(schedules);
        }


        public async Task<IEnumerable<ActivityScheduleResponseDto>> GetOptionalSchedulesByCampAsync(int campId)
        {
            var camp = await _unitOfWork.Camps.GetByIdAsync(campId)
                ?? throw new KeyNotFoundException("Camp not found.");
            var schedules = await _unitOfWork.ActivitySchedules.GetOptionalScheduleByCampIdAsync(campId);
            return _mapper.Map<IEnumerable<ActivityScheduleResponseDto>>(schedules);
        }

        public async Task<IEnumerable<ActivityScheduleResponseDto>> GetCoreSchedulesByCampAsync(int campId)
        {
            var camp = await _unitOfWork.Camps.GetByIdAsync(campId)
                ?? throw new KeyNotFoundException("Camp not found.");
            var schedules = await _unitOfWork.ActivitySchedules.GetCoreScheduleByCampIdAsync(campId);
            return _mapper.Map<IEnumerable<ActivityScheduleResponseDto>>(schedules);
        }

        public async Task<IEnumerable<ActivityScheduleResponseDto>> GetSchedulesByCampAsync(int campId)
        {
            var camp = await _unitOfWork.Camps.GetByIdAsync(campId)
                ?? throw new KeyNotFoundException("Camp not found.");
            var schedules = await _unitOfWork.ActivitySchedules.GetScheduleByCampIdAsync(campId);
            return _mapper.Map<IEnumerable<ActivityScheduleResponseDto>>(schedules);
        }

        public async Task<IEnumerable<ActivityScheduleResponseDto>> GetSchedulesByDateAsync(DateTime fromDate, DateTime toDate)
        {
            var fromUtc = fromDate.ToUtcForStorage();
            var toUtc = toDate.ToUtcForStorage();

            var schedules = await _unitOfWork.ActivitySchedules
                .GetActivitySchedulesByDateAsync(fromUtc, toUtc);

            var mapped = _mapper.Map<IEnumerable<ActivityScheduleResponseDto>>(schedules);

            foreach (var item in mapped)
            {
                item.StartTime = item.StartTime.ToVietnamTime();
                item.EndTime = item.EndTime.ToVietnamTime();
            }

            return mapped;
        }

        public async Task<ActivityScheduleResponseDto> ChangeStatusActivitySchedule(int activityScheduleId, ActivityScheduleStatus status)
        {
            var schedule = await _unitOfWork.ActivitySchedules.GetScheduleById(activityScheduleId)
            ?? throw new KeyNotFoundException("ActivitySchedule not found");

            schedule.status = status.ToString();
            await _unitOfWork.ActivitySchedules.UpdateAsync(schedule);
            await _unitOfWork.CommitAsync();

            return _mapper.Map<ActivityScheduleResponseDto>(schedule);
        }


        public async Task<ActivityScheduleResponseDto> UpdateLiveStreamStatus(int activityScheduleId, bool status)
        {
            var schedule = await _unitOfWork.ActivitySchedules.GetScheduleById(activityScheduleId)
            ?? throw new NotFoundException("ActivitySchedule not found");

            var activity = await _unitOfWork.Activities.GetByIdAsync(schedule.activityId)
              ?? throw new NotFoundException("Activity not found");

            var camp = await _unitOfWork.Camps.GetByIdAsync(activity.campId.Value)
                ?? throw new NotFoundException("Camp not found");

            if (schedule.isLivestream == status)
                throw new BusinessRuleException($"Live stream status is {status} now");

            if (schedule.startTime <= DateTime.UtcNow)
                throw new BusinessRuleException("STATUS CANNOT BE UPDATED BECAUSE SCHEDULE IS IN PROGRESS OR HAS EXPIRED");

            if (status)
            {
                var livestream = new Livestream
                {
                    title = $"{activity.name} - {camp.name}",
                    hostId = schedule.staffId
                };

                await _unitOfWork.LiveStreams.CreateAsync(livestream);
                await _unitOfWork.CommitAsync();
                schedule.isLivestream = status;
                schedule.livestreamId = livestream.livestreamId;
            }
            else
            {
                schedule.isLivestream = status;
                schedule.livestreamId = null;
            }

            await _unitOfWork.ActivitySchedules.UpdateAsync(schedule);
            await _unitOfWork.CommitAsync();

            var updateSchedule = await _unitOfWork.ActivitySchedules.GetScheduleById(activityScheduleId);

            return _mapper.Map<ActivityScheduleResponseDto>(updateSchedule);
        }

        public async Task<bool> DeleteActivityScheduleAsync(int activityScheduleId)
        {
            var schedule = await _unitOfWork.ActivitySchedules.GetByIdAsync(activityScheduleId)
                ?? throw new NotFoundException("Activity schedule not found.");
            var nowUtc = DateTime.UtcNow;
            if (schedule.startTime <= nowUtc)
                throw new BusinessRuleException("Cannot delete an activity schedule that is in progress or has already occurred.");
            await _unitOfWork.ActivitySchedules.RemoveAsync(schedule);
            await _unitOfWork.CommitAsync();
            return true;
        }

        public async Task ChangeActivityScheduleStatusAuto()
        {
            var schedules = await _unitOfWork.ActivitySchedules.GetAllAsync();
                        
            foreach (var schedule in schedules)
            {
                if (schedule.status == ActivityScheduleStatus.NotYet.ToString() &&
                    schedule.startTime.HasValue && schedule.startTime.Value <= DateTime.UtcNow)
                {
                    schedule.status = ActivityScheduleStatus.PendingAttendance.ToString();
                    await _unitOfWork.ActivitySchedules.UpdateAsync(schedule);
                }

                if ((schedule.status == ActivityScheduleStatus.AttendanceChecked.ToString()
                    || schedule.status == ActivityScheduleStatus.PendingAttendance.ToString())
                    && schedule.endTime.HasValue && schedule.endTime.Value <= DateTime.UtcNow
                    )
                {
                    schedule.status = ActivityScheduleStatus.Completed.ToString();
                    await _unitOfWork.ActivitySchedules.UpdateAsync(schedule);
                }
            }
            await _unitOfWork.CommitAsync();
        }

        public async Task ChangeActityScheduleToPendingAttendance()
        {
            var schedules = await _unitOfWork.ActivitySchedules.GetAllAsync();

            foreach (var schedule in schedules)
            {
                if (schedule.status == ActivityScheduleStatus.NotYet.ToString())
                {
                    schedule.status = ActivityScheduleStatus.PendingAttendance.ToString();
                    await _unitOfWork.ActivitySchedules.UpdateAsync(schedule);
                }
            }
            await _unitOfWork.CommitAsync();
        }
    }
}
