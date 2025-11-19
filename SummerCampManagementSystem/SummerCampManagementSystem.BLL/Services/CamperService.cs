using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.BLL.DTOs.Camper;
using SummerCampManagementSystem.BLL.Helpers;
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
    public class CamperService : ICamperService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUploadSupabaseService _uploadSupabaseService;
        private readonly IMapper _mapper;


        public CamperService(IUnitOfWork unitOfWork, IMapper mapper, IUploadSupabaseService uploadSupabaseService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _uploadSupabaseService = uploadSupabaseService;
        }

        public async Task<IEnumerable<CamperResponseDto>> GetByParentIdAsync(int parentId)
        {
            var campers = await _unitOfWork.ParentCampers.GetByParentIdAsync(parentId);
            return _mapper.Map<IEnumerable<CamperResponseDto>>(campers);
        }

        public async Task<CamperResponseDto> CreateCamperAsync(CamperRequestDto dto, int parentId)
        {
            if (dto.Dob >= new DateOnly(2019, 12, 1))
                throw new ArgumentException("Date of birth must be before 01/12/2019.");

            using var transaction = await _unitOfWork.BeginTransactionAsync();

            var camper = _mapper.Map<Camper>(dto);
            await _unitOfWork.Campers.CreateAsync(camper);
            await _unitOfWork.CommitAsync(); // cần commit 1 lần để lấy camperId

            if (dto.avatar != null)
            {
                var url = await _uploadSupabaseService.UploadCamperPhotoAsync(camper.camperId, dto.avatar);
                camper.avatar = url;
            }

            if (dto.HealthRecord != null)
            {
                var healthRecord = _mapper.Map<HealthRecord>(dto.HealthRecord);
                healthRecord.camperId = camper.camperId;
                healthRecord.createAt = TimezoneHelper.GetVietnamNow();

                await _unitOfWork.HealthRecords.CreateAsync(healthRecord);
                camper.HealthRecord = healthRecord;
            }

            var parentCamper = new ParentCamper
            {
                parentId = parentId,
                camperId = camper.camperId
            };
            await _unitOfWork.ParentCampers.CreateAsync(parentCamper);

            // Commit tất cả một lần
            await _unitOfWork.CommitAsync();
            await transaction.CommitAsync();

            return _mapper.Map<CamperResponseDto>(camper);
        }


        public async Task<bool> DeleteCamperAsync(int id)
        {
            var camper = await _unitOfWork.Campers.GetByIdAsync(id);
            if (camper == null) return false;

            await _unitOfWork.Campers.RemoveAsync(camper);
            await _unitOfWork.CommitAsync();
            return true;
        }

        public async Task<IEnumerable<CamperResponseDto>> GetAllCampersAsync()
        {
            var campers = await _unitOfWork.Campers.GetAllAsync();
            return _mapper.Map<IEnumerable<CamperResponseDto>>(campers);
        }

        public async Task<IEnumerable<CamperWithGuardiansResponseDto>> GetGuardiansByCamperId(int camperId)
        {
            var camper = await _unitOfWork.Campers.GetByIdAsync(camperId)
                ?? throw new KeyNotFoundException($"Camper with id {camperId} not found.");
            var guardians = await _unitOfWork.Campers.GetGuardiansByCamperId(camperId);
            return _mapper.Map<IEnumerable<CamperWithGuardiansResponseDto>>(guardians);
        }

        public async Task<IEnumerable<CamperSummaryDto>> GetCampersByOptionalActivitySchedule(int optionalActivityId)
        {
            var activity = await _unitOfWork.ActivitySchedules.GetByIdAsync(optionalActivityId)
                ?? throw new KeyNotFoundException($"Activity Schedule with id {optionalActivityId} not found.");
          
            var campers = await _unitOfWork.Campers.GetCampersByOptionalActivityId(optionalActivityId);
            return _mapper.Map<IEnumerable<CamperSummaryDto>>(campers);
        }

        public async Task<IEnumerable<CamperSummaryDto>> GetCampersByCoreScheduleAndStaffAsync(int coreActivityId, int staffId)
        {
            var core = await _unitOfWork.ActivitySchedules.GetByIdAsync(coreActivityId)
                ?? throw new KeyNotFoundException("Core activity not found.");

            var campersInCore = await _unitOfWork.Campers.GetCampersByCoreScheduleAndStaffAsync(coreActivityId, staffId);

            var optional = await _unitOfWork.ActivitySchedules.GetOptionalByCoreAsync(coreActivityId);

            if(optional == null)
            {
                return _mapper.Map<IEnumerable<CamperSummaryDto>>(campersInCore);
            }

            var camperIdsInOptional = await _unitOfWork.CamperActivities.GetCamperIdsInOptionalAsync(optional.activityScheduleId);

            var finalCampers = campersInCore
                .Where(c => !camperIdsInOptional.Contains(c.camperId))
                .ToList();

            return _mapper.Map<IEnumerable<CamperSummaryDto>>(finalCampers);
        }

        public async Task<IEnumerable<CamperSummaryDto>> GetCampersByCoreActivityIdAsync(int coreActivityId)
        {
            var core = await _unitOfWork.ActivitySchedules.GetByIdAsync(coreActivityId)
                ?? throw new KeyNotFoundException("Core activity not found.");

            var campersInCore = await _unitOfWork.Campers.GetCampersByCoreScheduleIdAsync(coreActivityId);

            var optional = await _unitOfWork.ActivitySchedules.GetOptionalByCoreAsync(coreActivityId);

            if (optional == null)
            {
                return _mapper.Map<IEnumerable<CamperSummaryDto>>(campersInCore);
            }

            var camperIdsInOptional = await _unitOfWork.CamperActivities.GetCamperIdsInOptionalAsync(optional.activityScheduleId);

            var finalCampers = campersInCore
                .Where(c => !camperIdsInOptional.Contains(c.camperId))
                .ToList();

            return _mapper.Map<IEnumerable<CamperSummaryDto>>(finalCampers);
        }



        public async Task<CamperResponseDto?> GetCamperByIdAsync(int id)
        {
            var camper = await _unitOfWork.Campers.GetByIdAsync(id);
            return camper == null ? null : _mapper.Map<CamperResponseDto>(camper);

        }

        public async Task<IEnumerable<CamperWithRegistrationStatus>> GetCampersByCampWithStatus(int campId)
        {
            var camp = await _unitOfWork.Camps.GetByIdAsync(campId)
                ?? throw new KeyNotFoundException($"Camp with id {campId} not found.");

            return await _unitOfWork.RegistrationCampers.GetQueryable()
                .Where(rc => rc.registration.campId == campId
                          && rc.registration.status != "PendingApproval"
                          && rc.registration.status != "Rejected")
                .Select(rc => new CamperWithRegistrationStatus
                {
                    CamperId = rc.camper.camperId,
                    CamperName = rc.camper.camperName,
                    Gender = rc.camper.gender,
                    Dob = rc.camper.dob,
                    avatar = rc.camper.avatar,
                    CamperRegistrationStatus = rc.status
                })
                .ToListAsync();
        }

        public async Task<CamperWithRegistrationStatus?> GetCamperByCampAndIdWithStatus(int campId, int camperId)
        {
            // Kiểm tra camp tồn tại
            var camp = await _unitOfWork.Camps.GetByIdAsync(campId)
                ?? throw new KeyNotFoundException($"Camp with id {campId} not found.");
            var camper = await _unitOfWork.Campers.GetByIdAsync(camperId)
                ?? throw new KeyNotFoundException($"Camper with id {camperId} not found.");

            var result = await _unitOfWork.RegistrationCampers.GetQueryable()
                .Where(rc =>
                    rc.registration.campId == campId &&
                    rc.camper.camperId == camperId
                   )
                .Select(rc => new CamperWithRegistrationStatus
                {
                    CamperId = rc.camper.camperId,
                    CamperName = rc.camper.camperName,
                    Gender = rc.camper.gender,
                    Dob = rc.camper.dob,
                    avatar = rc.camper.avatar,
                    CamperRegistrationStatus = rc.status
                })
                .FirstOrDefaultAsync();

            if (result == null)
                throw new KeyNotFoundException($"Camper with id {camperId} not found in camp {campId} or registration pending approval. ");
            return result;
        }

        public async Task<bool> UpdateCamperAsync(int id, CamperRequestDto dto)
        {
            var existingCamper = await _unitOfWork.Campers.GetByIdAsync(id);
            if (existingCamper == null) return false;

            _mapper.Map(dto, existingCamper);
           
            await _unitOfWork.Campers.UpdateAsync(existingCamper);

            if (dto.HealthRecord != null)
            {
                var existingHealthRecord = existingCamper.HealthRecord;

                if (existingHealthRecord == null)
                {
                    // Thêm mới nếu chưa có
                    var newRecord = _mapper.Map<HealthRecord>(dto.HealthRecord);
                    newRecord.camperId = existingCamper.camperId;
                    await _unitOfWork.HealthRecords.CreateAsync(newRecord);
                }
                else
                {
                    // Cập nhật nếu đã có
                    _mapper.Map(dto.HealthRecord, existingHealthRecord);
                    await _unitOfWork.HealthRecords.UpdateAsync(existingHealthRecord);
                }
            }
            await _unitOfWork.CommitAsync();


            return true;
        }


    }
}
