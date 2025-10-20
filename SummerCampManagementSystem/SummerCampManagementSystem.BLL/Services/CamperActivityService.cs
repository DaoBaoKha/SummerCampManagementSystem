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

        public async Task<CamperActivityResponseDto> RegisterOptionalActivityAsync(CamperActivityCreateDto dto)
        {
            // 1. Kiểm tra camper
            var camper = await _unitOfWork.Campers.GetByIdAsync(dto.CamperId);
            if (camper == null)
                throw new KeyNotFoundException("Camper not found.");

            // 2. Lấy registration của camper
            var registrationCamper = await _unitOfWork.Campers
                .GetRegistrationByCamperIdAsync(dto.CamperId);

            if (registrationCamper == null)
                throw new InvalidOperationException("Camper is not registered in any camp.");

            //var registration = registrationCamper.registration;

            // 3. Check thanh toán
            if (registrationCamper.status != "PendingCompletion")
                throw new InvalidOperationException("You must complete payment before selecting optional activities.");

            // 4. Lấy activity
            var activity = await _unitOfWork.Activities.GetByIdAsync(dto.ActivityId);
            if (activity == null)
                throw new KeyNotFoundException("Activity not found.");

            // 5. Check campId
            if (activity.campId != registrationCamper.campId)
                throw new InvalidOperationException("Selected activity does not belong to camper's camp.");

            // 6. Check type
            if (!string.Equals(activity.activityType, "Optional", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("You can only select optional activities.");

            // 7. Check duplicate
            bool exists = await _unitOfWork.CamperActivities.IsApprovedAsync(dto.CamperId, dto.ActivityId);
            if (exists)
                throw new InvalidOperationException("Camper already registered for this activity.");

            // 8. Tạo mới
            var camperActivity = _mapper.Map<CamperActivity>(dto);


            await _unitOfWork.CamperActivities.CreateAsync(camperActivity);
            await _unitOfWork.CommitAsync();

            // 9. Lấy lại với include
            var camperActivityWithDetails = await _unitOfWork.CamperActivities
                .GetByIdAsync(camperActivity.camperActivityId);

            return _mapper.Map<CamperActivityResponseDto>(camperActivityWithDetails);
        }
    }
}
