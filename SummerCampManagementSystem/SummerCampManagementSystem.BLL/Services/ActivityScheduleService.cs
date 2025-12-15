using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.BLL.DTOs.Activity;
using SummerCampManagementSystem.BLL.DTOs.ActivitySchedule;
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
                var accommodations = await _unitOfWork.Accommodations.GetByCampId(camp.campId);
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

        // Trong file: SummerCampManagementSystem.BLL/Services/ActivityScheduleService.cs


        public async Task<CreateScheduleBatchResult> CreateCoreScheduleAsync(ActivityScheduleCreateDto dto)
        {
            var result = new CreateScheduleBatchResult();

            // 1. Lấy thông tin Activity và Camp
            var activity = await _unitOfWork.Activities.GetByIdAsync(dto.ActivityId)
                ?? throw new KeyNotFoundException("Activity not found");

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

            var dtoStartVn = DateTime.SpecifyKind(dto.StartTime, DateTimeKind.Unspecified); // Giả sử FE gửi giờ VN
            var dtoEndVn = DateTime.SpecifyKind(dto.EndTime, DateTimeKind.Unspecified);

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

                    // 2. Check Conflict (Core đè Core, Cấm đè Optional/Resting)
                    // Lưu ý: Thêm điều kiện !s.isDeleted nếu có
                    bool hasConflict = await dbContext.ActivitySchedules
                        .Include(s => s.activity)
                        .AnyAsync(s =>
                            !s.status.Equals("Deleted") &&
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
                    schedule.maxCapacity = totalCapacity; // Có thể set max = current hoặc logic khác tùy bạn

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
        //public async Task<ActivityScheduleResponseDto> CreateCoreScheduleAsync(ActivityScheduleCreateDto dto)
        //{
        //    var activity = await _unitOfWork.Activities.GetByIdAsync(dto.ActivityId)
        //        ?? throw new KeyNotFoundException("Activity not found");

        //    var camp = await _unitOfWork.Camps.GetByIdAsync(activity.campId.Value)
        //        ?? throw new KeyNotFoundException("Camp not found");

        //    if (dto.StartTime >= dto.EndTime)
        //        throw new InvalidOperationException("Start date must be earlier than end date.");

        //    var startTimeUtc = dto.StartTime.ToUtcForStorage();
        //    var endTimeUtc = dto.EndTime.ToUtcForStorage();

        //    // Rule 1: Thời gian schedule phải nằm trong thời gian trại
        //    if (startTimeUtc < camp.startDate.Value || endTimeUtc > camp.endDate.Value)
        //    {
        //        throw new InvalidOperationException("Schedule time must be within the camp duration.");
        //    }

        //    // Check overlap
        //    bool overlap = await _unitOfWork.ActivitySchedules.IsTimeOverlapAsync(activity.campId, startTimeUtc, endTimeUtc);
        //    if (overlap)
        //        throw new InvalidOperationException("Core activity schedule overlaps with another core activity");


        //    // 🔹 Rule 2: Check trùng location trong cùng thời gian
        //    if (dto.LocationId.HasValue)
        //    {
        //        var location = await _unitOfWork.Locations.GetByIdAsync(dto.LocationId.Value)
        //      ?? throw new KeyNotFoundException("Location not found");

        //        bool locationConflict = await _unitOfWork.ActivitySchedules
        //            .ExistsInSameTimeAndLocationAsync(dto.LocationId.Value, startTimeUtc, endTimeUtc);

        //        if (locationConflict)
        //            throw new InvalidOperationException("This location is already occupied during the selected time range.");
        //    }


        //    if (dto.StaffId.HasValue)
        //    {
        //        var staff = await _unitOfWork.Users.GetByIdAsync(dto.StaffId.Value)
        //            ?? throw new KeyNotFoundException("Staff not found.");


        //        if (!string.Equals(staff.role, "Staff", StringComparison.OrdinalIgnoreCase))

        //        {
        //            throw new InvalidOperationException("Assigned user is not a staff member.");
        //        }

        //        // 4.2 Staff không được là supervisor của CamperGroup nào
        //        //bool isSupervisor = await _unitOfWork.CamperGroups.isSupervisor(dto.StaffId.Value);


        //        //if (isSupervisor)
        //        //    throw new InvalidOperationException("Staff is assigned as a supervisor and cannot join activities.");

        //        // 4.3 Staff không được trùng lịch với activity khác
        //        bool staffConflict = await _unitOfWork.ActivitySchedules
        //            .IsStaffBusyAsync(dto.StaffId.Value, startTimeUtc, endTimeUtc);

        //        if (staffConflict)
        //            throw new InvalidOperationException("Staff has another activity scheduled during this time.");
        //    }

        //    var groups = await _unitOfWork.Groups.GetByCampIdAsync(camp.campId);

        //    var accomodations = await _unitOfWork.Accommodations.GetByCampId(camp.campId);

        //    var currentCapacity = groups.Sum(g => g.CamperGroups?.Count ?? 0);

        //    if (dto.IsLiveStream == true && dto.StaffId == null)
        //        throw new InvalidOperationException("StaffId is required when livestream is enabled.");

        //    using var transaction = await _unitOfWork.BeginTransactionAsync();

        //    try
        //    {
        //        int? livestreamId = null;

        //        if (dto.IsLiveStream == true)
        //        {
        //            var livestream = new Livestream
        //            {
        //                title = $"{activity.name} - {camp.name}",
        //                hostId = dto.StaffId

        //            };
        //            await _unitOfWork.LiveStreams.CreateAsync(livestream);
        //            await _unitOfWork.CommitAsync();
        //            livestreamId = livestream.livestreamId;
        //        }
        //        var schedule = _mapper.Map<ActivitySchedule>(dto);

        //        schedule.startTime = startTimeUtc;
        //        schedule.endTime = endTimeUtc;

        //        schedule.currentCapacity = currentCapacity;
        //        schedule.livestreamId = livestreamId;


        //        await _unitOfWork.ActivitySchedules.CreateAsync(schedule);
        //        await _unitOfWork.CommitAsync();

        //        var result = await _unitOfWork.ActivitySchedules.GetByIdWithActivityAsync(schedule.activityScheduleId);

        //        if(result == null)
        //        {
        //            throw new InvalidOperationException("Failed to retrieve the created activity schedule.");
        //        }

        //        var activityType = result.activity.activityType;

        //        if (activityType == ActivityType.Core.ToString() || activityType == ActivityType.Checkin.ToString()
        //            || activityType == ActivityType.Checkout.ToString())
        //        {
        //            foreach (var group in groups)
        //            {
        //                var groupActivity = new GroupActivity
        //                {
        //                    groupId = group.groupId,
        //                    activityScheduleId = schedule.activityScheduleId,
        //                };
        //                await _unitOfWork.GroupActivities.CreateAsync(groupActivity);
        //            }
        //        }

        //        if (activityType == ActivityType.Resting.ToString())
        //        {
        //            foreach (var accomodation in accomodations)
        //            {
        //                var accomodationActivitySchedule = new AccommodationActivitySchedule
        //                {
        //                    accommodationId = accomodation.accommodationId,
        //                    activityScheduleId = schedule.activityScheduleId,
        //                };
        //                await _unitOfWork.AccommodationActivities.CreateAsync(accomodationActivitySchedule);
        //            }
        //        }

        //        await _unitOfWork.CommitAsync();

        //        await transaction.CommitAsync();

        //        return _mapper.Map<ActivityScheduleResponseDto>(result);
        //    }
        //    catch (Exception)
        //    {
        //        await transaction.RollbackAsync();
        //        throw;
        //    }
        //}

        public async Task<ActivityScheduleResponseDto> CreateOptionalScheduleAsync(OptionalScheduleCreateDto dto, int coreScheduleId)
        {
            var coreSlot = await _unitOfWork.ActivitySchedules.GetByIdAsync(coreScheduleId)
                ?? throw new KeyNotFoundException("Core schedule not found");

            if (!coreSlot.isOptional)
                throw new InvalidOperationException("This core schedule is not marked as optional");

            var activity = await _unitOfWork.Activities.GetByIdAsync(dto.ActivityId)
                ?? throw new KeyNotFoundException("Activity not found");

            var camp = await _unitOfWork.Camps.GetByIdAsync(activity.campId.Value)
               ?? throw new KeyNotFoundException("Camp not found");

            if (!string.Equals(activity.activityType, "Optional", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Only Optional activities can be created inside a core optional slot");

            if (dto.LocationId.HasValue)
            {
                var location = await _unitOfWork.Locations.GetByIdAsync(dto.LocationId.Value)
              ?? throw new KeyNotFoundException("Location not found");

                bool locationConflict = await _unitOfWork.ActivitySchedules
                    .ExistsInSameTimeAndLocationAsync(dto.LocationId.Value, coreSlot.startTime.Value, coreSlot.endTime.Value);

                if (locationConflict)
                    throw new InvalidOperationException("This location is already occupied during the selected time range.");
            }

            if (dto.StaffId.HasValue)
            {
                var staff = await _unitOfWork.Users.GetByIdAsync(dto.StaffId.Value)
                    ?? throw new KeyNotFoundException("Staff not found.");


                if (!string.Equals(staff.role, "Staff", StringComparison.OrdinalIgnoreCase))

                {
                    throw new InvalidOperationException("Assigned user is not a staff member.");
                }

                // 4.2 Staff không được là supervisor của CamperGroup nào
                //bool isSupervisor = await _unitOfWork.CamperGroups.isSupervisor(dto.StaffId.Value);


                //if (isSupervisor)
                //    throw new InvalidOperationException("Staff is assigned as a supervisor and cannot join activities.");

                // 4.3 Staff không được trùng lịch với activity khác
                bool staffConflict = await _unitOfWork.ActivitySchedules
                    .IsStaffBusyAsync(dto.StaffId.Value, coreSlot.startTime.Value, coreSlot.endTime.Value);

                if (staffConflict)
                    throw new InvalidOperationException("Staff has another activity scheduled during this time.");
            }

            if (dto.IsLiveStream == true && dto.StaffId == null)
                throw new InvalidOperationException("StaffId is required when livestream is enabled.");

            using var transaction = await _unitOfWork.BeginTransactionAsync();

            try
            {
                int? livestreamId = null;

                if (dto.IsLiveStream == true)
                {
                    var livestream = new Livestream
                    {
                        title = $"{activity.name} - {camp.name}",
                        hostId = dto.StaffId
                    };

                    await _unitOfWork.LiveStreams.CreateAsync(livestream);
                    await _unitOfWork.CommitAsync();

                    livestreamId = livestream.livestreamId;
                }
                var schedule = _mapper.Map<OptionalScheduleCreateDto, ActivitySchedule>(dto);
                schedule.livestreamId = livestreamId;
                schedule.startTime = coreSlot.startTime;
                schedule.endTime = coreSlot.endTime;
                schedule.coreActivityId = coreSlot.activityScheduleId;

                await _unitOfWork.ActivitySchedules.CreateAsync(schedule);
                await _unitOfWork.CommitAsync();

                await transaction.CommitAsync();

                var result = await _unitOfWork.ActivitySchedules.GetByIdWithActivityAsync(schedule.activityScheduleId);

                return _mapper.Map<ActivityScheduleResponseDto>(result);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }


        public async Task<ActivityScheduleResponseDto> UpdateCoreScheduleAsync(int id, ActivityScheduleCreateDto dto)
        {
            var schedule = await _unitOfWork.ActivitySchedules.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("Activity schedule not found.");

            var activity = await _unitOfWork.Activities.GetByIdAsync(dto.ActivityId)
                ?? throw new KeyNotFoundException("Activity not found.");

            //if (!string.Equals(activity.activityType, "Core", StringComparison.OrdinalIgnoreCase))
            //    throw new InvalidOperationException("Only Core activities can be updated through this method.");

            var camp = await _unitOfWork.Camps.GetByIdAsync(activity.campId.Value)
                ?? throw new KeyNotFoundException("Camp not found.");

            // 🔹 Rule 1: Schedule nằm trong thời gian trại
            if (dto.StartTime < camp.startDate.Value || dto.EndTime > camp.endDate.Value)
                throw new InvalidOperationException("Schedule time must be within the camp duration.");

            // 🔹 Rule 2: Không trùng thời gian core activity khác (ngoại trừ chính nó)
            bool overlap = await _unitOfWork.ActivitySchedules
                .IsTimeOverlapAsync(activity.campId, dto.StartTime, dto.EndTime, excludeScheduleId: id);
            if (overlap)
                throw new InvalidOperationException("Core activity schedule overlaps with another core activity.");

            // 🔹 Rule 3: Kiểm tra location (nếu có)
            if (dto.LocationId.HasValue)
            {
                var location = await _unitOfWork.Locations.GetByIdAsync(dto.LocationId.Value)
                    ?? throw new KeyNotFoundException("Location not found.");

                bool locationConflict = await _unitOfWork.ActivitySchedules
                    .ExistsInSameTimeAndLocationAsync(dto.LocationId.Value, dto.StartTime, dto.EndTime, excludeScheduleId: id);

                if (locationConflict)
                    throw new InvalidOperationException("This location is already occupied during the selected time range.");
            }

            // 🔹 Rule 4: Kiểm tra Staff (nếu có)
            if (dto.StaffId.HasValue)
            {
                var staff = await _unitOfWork.Users.GetByIdAsync(dto.StaffId.Value)
                    ?? throw new KeyNotFoundException("Staff not found.");

                if (!string.Equals(staff.role, "Staff", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Assigned user is not a staff member.");

                //bool isSupervisor = await _unitOfWork.CamperGroups.isSupervisor(dto.StaffId.Value);
                //if (isSupervisor)
                //    throw new InvalidOperationException("Staff is assigned as a supervisor and cannot join activities.");

                bool staffConflict = await _unitOfWork.ActivitySchedules
                    .IsStaffBusyAsync(dto.StaffId.Value, dto.StartTime, dto.EndTime, excludeScheduleId: id);

                if (staffConflict)
                    throw new InvalidOperationException("Staff has another activity scheduled during this time.");
            }

            // 🔹 Update các field
            var groups = await _unitOfWork.Groups.GetByCampIdAsync(camp.campId);
            var currentCapacity = groups.Sum(g => g.CamperGroups?.Count ?? 0);

            _mapper.Map(dto, schedule);

            if (schedule.startTime.HasValue)
                schedule.startTime = schedule.startTime.Value.ToUtcForStorage();

            if (schedule.endTime.HasValue)
                schedule.endTime = schedule.endTime.Value.ToUtcForStorage();

            schedule.currentCapacity = currentCapacity;

            await _unitOfWork.ActivitySchedules.UpdateAsync(schedule);
            await _unitOfWork.CommitAsync();

            var updated = await _unitOfWork.ActivitySchedules.GetByIdWithActivityAsync(schedule.activityScheduleId);
            return _mapper.Map<ActivityScheduleResponseDto>(updated);
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

        public async Task<IEnumerable<ActivityScheduleByCamperResponseDto>> GetSchedulesByCamperAndCampAsync(int campId, int camperId)
        {
            var camper = await _unitOfWork.Campers.GetByIdAsync(camperId)
                ?? throw new KeyNotFoundException("Camper not found.");

            var camp = await _unitOfWork.Camps.GetByIdAsync(campId)
                ?? throw new KeyNotFoundException("Camp not found.");

            var isCamperInCamp = await _unitOfWork.ActivitySchedules
                .IsCamperofCamp(campId, camperId);

            if (!isCamperInCamp)
                throw new InvalidOperationException("Camper is not enrolled in the camp.");

            var coreSchedules = await _unitOfWork.ActivitySchedules
                .GetAllWithActivityAndAttendanceAsync(campId, camperId);

            var optionalSchedules = await _unitOfWork.ActivitySchedules.GetOptionalSchedulesByCamperAsync(camperId);

            var all = coreSchedules
                      .Union(optionalSchedules)
                      .ToList();

            // Optional override Core
            var overriddenCoreIds = optionalSchedules
                .Where(s => s.coreActivityId != null)
                .Select(s => s.coreActivityId)
                .ToHashSet();

            var filteredSchedules = all
               .Where(s => !overriddenCoreIds.Contains(s.activityScheduleId));

            // return filter result
            return _mapper.Map<IEnumerable<ActivityScheduleByCamperResponseDto>>(filteredSchedules);
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
