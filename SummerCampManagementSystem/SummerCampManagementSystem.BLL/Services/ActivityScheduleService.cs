using AutoMapper;
using SummerCampManagementSystem.BLL.DTOs.Activity;
using SummerCampManagementSystem.BLL.DTOs.ActivitySchedule;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.Core.Enums;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;

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
            var activities = await _unitOfWork.ActivitySchedules.GetAllAsync();
            return _mapper.Map<IEnumerable<ActivityScheduleResponseDto>>(activities);
        }

        public async Task<object> GetAllSchedulesByStaffIdAsync(int staffId)
        {
            var schedules = await _unitOfWork.ActivitySchedules.GetAllSchedulesByStaffIdAsync(staffId);
               
            return new
            {
                ActivitySchedules = schedules
              .GroupBy(a => new { a.activity.campId, a.activity.camp.name })
              .Select(g => new
              {
                  g.Key.campId,
                  campName = g.Key.name,
                  activities = g.Select(a => new
                  {
                      a.activityScheduleId,
                      a.activity.name,
                      a.startTime,
                      a.endTime,
                      location = a.location.name
                  })
              })
            };
        }


        public async Task<ActivityScheduleResponseDto> CreateCoreScheduleAsync(ActivityScheduleCreateDto dto)
        {
            var activity = await _unitOfWork.Activities.GetByIdAsync(dto.ActivityId)
                ?? throw new KeyNotFoundException("Activity not found");

            if (!string.Equals(activity.activityType, "Core", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Only Core activities can have a core schedule");


            var camp = await _unitOfWork.Camps.GetByIdAsync(activity.campId.Value)
                ?? throw new KeyNotFoundException("Camp not found");

            if (dto.StartTime >= dto.EndTime)
                throw new InvalidOperationException("Start date must be earlier than end date.");

            // Rule 1: Thời gian schedule phải nằm trong thời gian trại
            if (dto.StartTime < camp.startDate.Value || dto.EndTime > camp.endDate.Value)
            {
                throw new InvalidOperationException("Schedule time must be within the camp duration.");
            }

            // Check overlap
            bool overlap = await _unitOfWork.ActivitySchedules.IsTimeOverlapAsync(activity.campId, dto.StartTime, dto.EndTime);
            if (overlap)
                throw new InvalidOperationException("Core activity schedule overlaps with another core activity");


            // 🔹 Rule 2: Check trùng location trong cùng thời gian
            if (dto.locationId.HasValue)
            {
                var location = await _unitOfWork.Locations.GetByIdAsync(dto.locationId.Value)
              ?? throw new KeyNotFoundException("Location not found");

                bool locationConflict = await _unitOfWork.ActivitySchedules
                    .ExistsInSameTimeAndLocationAsync(dto.locationId.Value, dto.StartTime, dto.EndTime);

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
                bool isSupervisor = await _unitOfWork.CamperGroups.isSupervisor(dto.StaffId.Value);


                if (isSupervisor)
                    throw new InvalidOperationException("Staff is assigned as a supervisor and cannot join activities.");

                // 4.3 Staff không được trùng lịch với activity khác
                bool staffConflict = await _unitOfWork.ActivitySchedules
                    .IsStaffBusyAsync(dto.StaffId.Value, dto.StartTime, dto.EndTime);

                if (staffConflict)
                    throw new InvalidOperationException("Staff has another activity scheduled during this time.");
            }

            var groups = await _unitOfWork.CamperGroups.GetByCampIdAsync(camp.campId);
            var currentCapacity = groups.Sum(g => g.Campers?.Count ?? 0);

            var schedule = _mapper.Map<ActivitySchedule>(dto);

            schedule.currentCapacity = currentCapacity;


            await _unitOfWork.ActivitySchedules.CreateAsync(schedule);
            await _unitOfWork.CommitAsync();

            foreach (var group in groups)
            {
                var groupActivity = new GroupActivity
                {
                    camperGroupId = group.camperGroupId,
                    activityScheduleId = schedule.activityScheduleId,
                    //status = "Pending"
                };
                await _unitOfWork.GroupActivities.CreateAsync(groupActivity);
            }
            await _unitOfWork.CommitAsync();

            var result = await _unitOfWork.ActivitySchedules.GetByIdWithActivityAsync(schedule.activityScheduleId);


            return _mapper.Map<ActivityScheduleResponseDto>(result);
        }

        public async Task<ActivityScheduleResponseDto> CreateOptionalScheduleAsync(OptionalScheduleCreateDto dto, int coreScheduleId)
        {
            var coreSlot = await _unitOfWork.ActivitySchedules.GetByIdAsync(coreScheduleId)
                ?? throw new KeyNotFoundException("Core schedule not found");

            if (!coreSlot.isOptional)
                throw new InvalidOperationException("This core schedule is not marked as optional");

            var activity = await _unitOfWork.Activities.GetByIdAsync(dto.ActivityId)
                ?? throw new KeyNotFoundException("Activity not found");

            if (!string.Equals(activity.activityType, "Optional", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Only Optional activities can be created inside a core optional slot");

            if (dto.locationId.HasValue)
            {
                var location = await _unitOfWork.Locations.GetByIdAsync(dto.locationId.Value)
              ?? throw new KeyNotFoundException("Location not found");

                bool locationConflict = await _unitOfWork.ActivitySchedules
                    .ExistsInSameTimeAndLocationAsync(dto.locationId.Value, coreSlot.startTime.Value, coreSlot.endTime.Value);

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
                bool isSupervisor = await _unitOfWork.CamperGroups.isSupervisor(dto.StaffId.Value);


                if (isSupervisor)
                    throw new InvalidOperationException("Staff is assigned as a supervisor and cannot join activities.");

                // 4.3 Staff không được trùng lịch với activity khác
                bool staffConflict = await _unitOfWork.ActivitySchedules
                    .IsStaffBusyAsync(dto.StaffId.Value, coreSlot.startTime.Value, coreSlot.endTime.Value);

                if (staffConflict)
                    throw new InvalidOperationException("Staff has another activity scheduled during this time.");
            }

            var schedule = _mapper.Map<OptionalScheduleCreateDto, ActivitySchedule>(dto);

            schedule.startTime = coreSlot.startTime;
            schedule.endTime = coreSlot.endTime;
            schedule.coreActivityId = coreSlot.activityScheduleId;



            await _unitOfWork.ActivitySchedules.CreateAsync(schedule);
            await _unitOfWork.CommitAsync();




            var result = await _unitOfWork.ActivitySchedules.GetByIdWithActivityAsync(schedule.activityScheduleId);

            return _mapper.Map<ActivityScheduleResponseDto>(result);
        }


        public async Task<ActivityScheduleResponseDto> UpdateCoreScheduleAsync(int id, ActivityScheduleCreateDto dto)
        {
            var schedule = await _unitOfWork.ActivitySchedules.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("Activity schedule not found.");

            var activity = await _unitOfWork.Activities.GetByIdAsync(dto.ActivityId)
                ?? throw new KeyNotFoundException("Activity not found.");

            if (!string.Equals(activity.activityType, "Core", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Only Core activities can be updated through this method.");

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
            if (dto.locationId.HasValue)
            {
                var location = await _unitOfWork.Locations.GetByIdAsync(dto.locationId.Value)
                    ?? throw new KeyNotFoundException("Location not found.");

                bool locationConflict = await _unitOfWork.ActivitySchedules
                    .ExistsInSameTimeAndLocationAsync(dto.locationId.Value, dto.StartTime, dto.EndTime, excludeScheduleId: id);

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

                bool isSupervisor = await _unitOfWork.CamperGroups.isSupervisor(dto.StaffId.Value);
                if (isSupervisor)
                    throw new InvalidOperationException("Staff is assigned as a supervisor and cannot join activities.");

                bool staffConflict = await _unitOfWork.ActivitySchedules
                    .IsStaffBusyAsync(dto.StaffId.Value, dto.StartTime, dto.EndTime, excludeScheduleId: id);

                if (staffConflict)
                    throw new InvalidOperationException("Staff has another activity scheduled during this time.");
            }

            // 🔹 Update các field
            var groups = await _unitOfWork.CamperGroups.GetByCampIdAsync(camp.campId);
            var currentCapacity = groups.Sum(g => g.Campers?.Count ?? 0);

            _mapper.Map(dto, schedule);

            schedule.currentCapacity = currentCapacity;

            await _unitOfWork.ActivitySchedules.UpdateAsync(schedule);
            await _unitOfWork.CommitAsync();

            var updated = await _unitOfWork.ActivitySchedules.GetByIdWithActivityAsync(schedule.activityScheduleId);
            return _mapper.Map<ActivityScheduleResponseDto>(updated);
        }

        public async Task<IEnumerable<ActivityScheduleResponseDto>> GetByCampAndStaffAsync(int campId, int staffId, ActivityScheduleType? status = null)
        {

            var camp = await _unitOfWork.Camps.GetByIdAsync(campId)
                ?? throw new KeyNotFoundException("Camp not found.");

            var staff = await _unitOfWork.Users.GetByIdAsync(staffId)
                ?? throw new KeyNotFoundException("Staff not found.");

            var schedules = await _unitOfWork.ActivitySchedules.GetByCampAndStaffAsync(campId, staffId, status);

            return _mapper.Map<IEnumerable<ActivityScheduleResponseDto>>(schedules);
        }

        public async Task<IEnumerable<ActivityScheduleByCamperResponseDto>> GetSchedulesByCamperAndCampAsync(int camperId, int campId)
        {
            var camper = await _unitOfWork.Campers.GetByIdAsync(camperId)
                ?? throw new KeyNotFoundException("Camper not found.");

            var camp = await _unitOfWork.Camps.GetByIdAsync(campId)
                ?? throw new KeyNotFoundException("Camp not found.");

            var schedules = await _unitOfWork.ActivitySchedules
                .GetAllWithActivityAndAttendanceAsync(campId, camperId);



            var joinedOptionalCoreIds = schedules
                .Where(s => s.coreActivityId != null)       // lọc những cái có coreActivityId
                .Select(s => s.coreActivityId)        // lấy giá trị int
                .ToHashSet();


            var filteredSchedules = schedules
                .Where(s => !joinedOptionalCoreIds.Contains(s.activityScheduleId))
                .ToList();

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
            var schedules = await _unitOfWork.ActivitySchedules
                .GetActivitySchedulesByDateAsync(fromDate, toDate);
            return _mapper.Map<IEnumerable<ActivityScheduleResponseDto>>(schedules);
        }
    }
}
