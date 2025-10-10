using AutoMapper;
using SummerCampManagementSystem.BLL.DTOs.Requests.Camper;
using SummerCampManagementSystem.BLL.DTOs.Responses.Camper;
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

        public async Task<CamperResponseDto> CreateCamperAsync(CamperCreateDto dto)
        {
            var camper = _mapper.Map<Camper>(dto);
            await _unitOfWork.Campers.CreateAsync(camper);
            await _unitOfWork.CommitAsync();
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

        public async Task<bool> UpdateCamperAsync(CamperUpdateDto dto)
        {
            var existingCamper = await _unitOfWork.Campers.GetByIdAsync(dto.CamperId);
            if (existingCamper == null) return false;

            _mapper.Map(dto, existingCamper);
            await _unitOfWork.Campers.UpdateAsync(existingCamper);
            await _unitOfWork.CommitAsync();
            return true;
        }
    }
}
