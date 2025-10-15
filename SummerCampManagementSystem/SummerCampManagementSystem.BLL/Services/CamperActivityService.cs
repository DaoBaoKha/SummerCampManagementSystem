using AutoMapper;
using SummerCampManagementSystem.BLL.DTOs.CamperActivity;
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
    public class CamperActivityService : ICamperActivityService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;


        public CamperActivityService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<CamperActivityResponseDto> CreateAsync(CamperActivityCreateDto dto)
        {
            var camperActivity = _mapper.Map<CamperActivity>(dto);
            await _unitOfWork.CamperActivities.CreateAsync(camperActivity);
            await _unitOfWork.CommitAsync();
            var created = await _unitOfWork.CamperActivities.GetByIdAsync(camperActivity.camperActivityId);

            return _mapper.Map<CamperActivityResponseDto>(created);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var activity = await _unitOfWork.CamperActivities.GetByIdAsync(id);
            if (activity == null) return false;

            await _unitOfWork.CamperActivities.RemoveAsync(activity);
            await _unitOfWork.CommitAsync();
            return true;
        }

        public async Task<IEnumerable<CamperActivityResponseDto>> GetAllAsync()
        {
            var camperActivities = await _unitOfWork.CamperActivities.GetAllAsync();
            return _mapper.Map<IEnumerable<CamperActivityResponseDto>>(camperActivities);
        }

        public async Task<CamperActivityResponseDto?> GetByIdAsync(int id)
        {
            var camperActivity = await _unitOfWork.CamperActivities.GetByIdAsync(id);
            return camperActivity == null ? null : _mapper.Map<CamperActivityResponseDto>(camperActivity);
        }

        public async Task<bool> UpdateAsync(int id, CamperActivityUpdateDto dto)
        {
            var activity = await _unitOfWork.CamperActivities.GetByIdAsync(id);
            if (activity == null) return false;

            _mapper.Map(dto, activity);
            await _unitOfWork.CamperActivities.UpdateAsync(activity);
            await _unitOfWork.CommitAsync();
            return true;
        }
    }
}
