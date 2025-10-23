using AutoMapper;
using SummerCampManagementSystem.BLL.DTOs.ActivitySchedule;
using SummerCampManagementSystem.BLL.Interfaces;
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

        public async Task<ActivityScheduleResponseDto> CreateCoreScheduleAsync(ActivityScheduleCreateDto dto)
        {
            var activity = await _unitOfWork.Activities.GetByIdAsync(dto.ActivityId)
                ?? throw new KeyNotFoundException("Activity not found");

            if (!string.Equals(activity.activityType, "Core", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Only Core activities can have a core schedule");
            // Check overlap
            bool overlap = await _unitOfWork.ActivitySchedules.IsTimeOverlapAsync(activity.campId, dto.StartTime, dto.EndTime);
            if (overlap)
                throw new InvalidOperationException("Core activity schedule overlaps with another core activity");

            var schedule = _mapper.Map<ActivitySchedule>(dto);
            schedule.isOptional = false;

            await _unitOfWork.ActivitySchedules.CreateAsync(schedule);
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

            // Check time within core slot
            if (dto.StartTime < coreSlot.startTime || dto.EndTime > coreSlot.endTime)
                throw new InvalidOperationException("Optional schedule must be within the core optional slot");

            var schedule = _mapper.Map<ActivitySchedule>(dto);

            await _unitOfWork.ActivitySchedules.CreateAsync(schedule);
            await _unitOfWork.CommitAsync();

            var result = await _unitOfWork.ActivitySchedules.GetByIdWithActivityAsync(schedule.activityScheduleId);

            return _mapper.Map<ActivityScheduleResponseDto>(result);
        }
    }
}
