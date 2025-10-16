using AutoMapper;
using SummerCampManagementSystem.BLL.DTOs.Camper;
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
        private readonly IMapper _mapper;


        public CamperService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<CamperResponseDto> CreateCamperAsync(CamperRequestDto dto)
        {
            var camper = _mapper.Map<Camper>(dto);
            await _unitOfWork.Campers.CreateAsync(camper);
            await _unitOfWork.CommitAsync();

            if (dto.HealthRecords != null)
            {
                var healthRecord = _mapper.Map<HealthRecord>(dto.HealthRecords);
                healthRecord.camperId = camper.camperId;
                healthRecord.createAt = DateTime.UtcNow;
                await _unitOfWork.HealthRecords.CreateAsync(healthRecord);
                await _unitOfWork.CommitAsync();

                //camper.HealthRecords = healthRecord;

                camper.HealthRecord = healthRecord;
            }

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

        public async Task<CamperResponseDto?> GetCamperByIdAsync(int id)
        {
            var camper = await _unitOfWork.Campers.GetByIdAsync(id);
            return camper == null ? null : _mapper.Map<CamperResponseDto>(camper);

        }

        public async Task<bool> UpdateCamperAsync(int id, CamperRequestDto dto)
        {
            var existingCamper = await _unitOfWork.Campers.GetByIdAsync(id);
            if (existingCamper == null) return false;

            _mapper.Map(dto, existingCamper);
           
            await _unitOfWork.Campers.UpdateAsync(existingCamper);

            if (dto.HealthRecords != null)
            {
                //sửa db
                var existingHealthRecord = existingCamper.HealthRecord;

                if (existingHealthRecord == null)
                {
                    // Thêm mới nếu chưa có
                    var newRecord = _mapper.Map<HealthRecord>(dto.HealthRecords);
                    newRecord.camperId = existingCamper.camperId;
                    await _unitOfWork.HealthRecords.CreateAsync(newRecord);
                }
                else
                {
                    // Cập nhật nếu đã có
                    _mapper.Map(dto.HealthRecords, existingHealthRecord);
                    //await _unitOfWork.HealthRecords.UpdateAsync(existingCamper.HealthRecord);

                    await _unitOfWork.HealthRecords.UpdateAsync(existingHealthRecord);
                }
            }
            await _unitOfWork.CommitAsync();


            return true;
        }
    }
}
