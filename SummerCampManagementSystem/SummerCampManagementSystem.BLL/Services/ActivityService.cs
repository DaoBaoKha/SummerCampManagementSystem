using AutoMapper;
using SummerCampManagementSystem.BLL.DTOs.Activity;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.Services
{
    public class ActivityService : IActivityService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public ActivityService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }
        public async Task<ActivityResponseDto> CreateAsync(ActivityCreateDto dto)
        {
            var activity = _mapper.Map<Activity>(dto);
            await _unitOfWork.Activities.CreateAsync(activity);
            await _unitOfWork.CommitAsync();
            return _mapper.Map<ActivityResponseDto>(activity);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var activity = await _unitOfWork.Activities.GetByIdAsync(id);
            if (activity == null) return false;

            await _unitOfWork.Activities.RemoveAsync(activity);
            await _unitOfWork.CommitAsync();
            return true;
        }

        public async Task<IEnumerable<ActivityResponseDto>> GetAllAsync()
        {
            var activities = await _unitOfWork.Activities.GetAllAsync();
            return _mapper.Map<IEnumerable<ActivityResponseDto>>(activities);
        }

        public async Task<IEnumerable<ActivityResponseDto>> GetByCampIdAsync(int campId)
        {
            var activities = await _unitOfWork.Activities.GetByCampIdAsync(campId);
            return _mapper.Map<IEnumerable<ActivityResponseDto>>(activities);
        }

        public async Task<ActivityResponseDto?> GetByIdAsync(int id)
        {
            var activity = await _unitOfWork.Activities.GetByIdAsync(id);
            return activity == null ? null : _mapper.Map<ActivityResponseDto> (activity);
        }

        public async Task<bool> UpdateAsync(int id, ActivityCreateDto dto)
        {
            var activity = await _unitOfWork.Activities.GetByIdAsync(id);
            if (activity == null) return false;

            _mapper.Map(dto, activity);
            await _unitOfWork.Activities.UpdateAsync(activity);
            await _unitOfWork.CommitAsync();
            return true;
        }

        public async Task<IEnumerable<ActivityResponseDto>> GetOptionalActivitiesAsync()
        {
            var activities = await _unitOfWork.Activities.GetOptionalActivitiesAsync();
            return _mapper.Map<IEnumerable<ActivityResponseDto>>(activities);
        }
    }
}
