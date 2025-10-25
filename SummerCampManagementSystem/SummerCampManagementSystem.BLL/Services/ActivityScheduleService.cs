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


        public async Task<ActivityScheduleResponseDto> CreateCoreScheduleAsync(ActivityScheduleCreateDto dto)
        {
            var activity = await _unitOfWork.Activities.GetByIdAsync(dto.ActivityId)
                ?? throw new KeyNotFoundException("Activity not found");

            if (!string.Equals(activity.activityType, "Core", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Only Core activities can have a core schedule");


            var camp = await _unitOfWork.Camps.GetByIdAsync(activity.campId.Value)
                ?? throw new KeyNotFoundException("Camp not found");

            // Rule 1: Thời gian schedule phải nằm trong thời gian trại
            if (dto.StartTime < camp.startDate.Value.ToDateTime(TimeOnly.MinValue) ||
                dto.EndTime > camp.endDate.Value.ToDateTime(TimeOnly.MaxValue))
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
                    status = "Pending" 
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


            var schedule = _mapper.Map<ActivitySchedule>(dto);

            schedule.startTime = coreSlot.startTime;
            schedule.endTime = coreSlot.endTime;
            schedule.roomId = coreSlot.activityScheduleId.ToString();

            await _unitOfWork.ActivitySchedules.CreateAsync(schedule);
            await _unitOfWork.CommitAsync();

            var result = await _unitOfWork.ActivitySchedules.GetByIdWithActivityAsync(schedule.activityScheduleId);

            return _mapper.Map<ActivityScheduleResponseDto>(result);
        }
    }
}
