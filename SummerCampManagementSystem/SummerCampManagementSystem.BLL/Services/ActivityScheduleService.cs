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

        public async Task<ActivityScheduleResponseDto> CreateCoreScheduleAsync(ActivityScheduleCreateDto dto)
        {
            var activity = await _unitOfWork.Activities.GetByIdAsync(dto.ActivityId)
                ?? throw new KeyNotFoundException("Activity not found");

            if (string.Equals(activity.activityType, "Optional", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Optional activities cannot have a core schedule");


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
            if (dto.LocationId.HasValue)
            {
                var location = await _unitOfWork.Locations.GetByIdAsync(dto.LocationId.Value)
              ?? throw new KeyNotFoundException("Location not found");

                bool locationConflict = await _unitOfWork.ActivitySchedules
                    .ExistsInSameTimeAndLocationAsync(dto.LocationId.Value, dto.StartTime, dto.EndTime);

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
                    .IsStaffBusyAsync(dto.StaffId.Value, dto.StartTime, dto.EndTime);

                if (staffConflict)
                    throw new InvalidOperationException("Staff has another activity scheduled during this time.");
            }

            var groups = await _unitOfWork.Groups.GetByCampIdAsync(camp.campId);
            var currentCapacity = groups.Sum(g => g.CamperGroups?.Count ?? 0);

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
                var schedule = _mapper.Map<ActivitySchedule>(dto);

                if (schedule.startTime.HasValue)
                    schedule.startTime = schedule.startTime.Value.ToUtcForStorage();

                if (schedule.endTime.HasValue)
                    schedule.endTime = schedule.endTime.Value.ToUtcForStorage();

                schedule.currentCapacity = currentCapacity;
                schedule.livestreamId = livestreamId;


                await _unitOfWork.ActivitySchedules.CreateAsync(schedule);
                await _unitOfWork.CommitAsync();

                foreach (var group in groups)
                {
                    var groupActivity = new GroupActivity
                    {
                        groupId = group.groupId,
                        activityScheduleId = schedule.activityScheduleId,
                    };
                    await _unitOfWork.GroupActivities.CreateAsync(groupActivity);
                }
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

            var staff = await _unitOfWork.Users.GetByIdAsync(staffId);

            var schedules = await _unitOfWork.ActivitySchedules.GetByCampAndStaffAsync(campId, staffId);

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

            var baseQuery = _unitOfWork.ActivitySchedules
                // use new IQueryable method from repo
                .GetQueryableWithBaseIncludes()
                .Where(s => s.activity.campId == campId);

            // use .ProjectTo
            var schedules = await baseQuery
                .ProjectTo<ActivityScheduleByCamperResponseDto>(
                    // use _mapper for ConfigurationProvider
                    _mapper.ConfigurationProvider,
                    // inject camperId
                    new Dictionary<string, object> { { "camperId", camperId } }
                )
                .ToListAsync();

            // final filter logic
            var joinedOptionalCoreIds = schedules
                .Where(s => s.CoreActivityId != null)
                .Select(s => s.ActivityScheduleId)
                .ToHashSet();

            // return filter result
            return schedules
                .Where(s => !joinedOptionalCoreIds.Contains(s.ActivityScheduleId))
                .ToList();
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
    }
}
