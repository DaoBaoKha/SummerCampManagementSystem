using AutoMapper;
using SummerCampManagementSystem.BLL.DTOs.Guardian;
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
    public class GuardianService : IGuardianService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICamperService _camperService;

        public GuardianService(IUnitOfWork unitOfWork, IMapper mapper, ICamperService camperService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _camperService = camperService;
        }

        public async Task<GuardianResponseDto> CreateAsync(GuardianCreateDto dto, int camperId)
        {
            if (dto.Dob >= new DateOnly(2007, 12, 1))
                throw new ArgumentException("Date of birth must be before 01/12/2007.");

            var guardian = _mapper.Map<Guardian>(dto);
            await _unitOfWork.Guardians.CreateAsync(guardian);
            await _unitOfWork.CommitAsync();

            var camper = _unitOfWork.Campers.GetByIdAsync(camperId)
                ?? throw new KeyNotFoundException($"Camper with id {camperId} not found");

            var camperGuardians = new CamperGuardian
            {
                guardianId = guardian.guardianId,
                camperId = camperId
            };

            await _unitOfWork.CamperGuardians.CreateAsync(camperGuardians);
            await _unitOfWork.CommitAsync();

            var currentGuardian = await _unitOfWork.Guardians.GetByIdAsync(guardian.guardianId);

            return _mapper.Map<GuardianResponseDto>(currentGuardian);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var guardian = await _unitOfWork.Guardians.GetByIdAsync(id);
            if (guardian == null) return false;

            await _unitOfWork.Guardians.RemoveAsync(guardian);
            await _unitOfWork.CommitAsync();
            return true;
        }

        public async Task<IEnumerable<GuardianResponseDto>> GetAllAsync()
        {
            var list = await _unitOfWork.Guardians.GetAllAsync();
            return _mapper.Map<IEnumerable<GuardianResponseDto>>(list);
        }

        public async Task<GuardianResponseDto?> GetByIdAsync(int id)
        {
            var guardian = await _unitOfWork.Guardians.GetByIdAsync(id);
            return guardian == null ? null : _mapper.Map<GuardianResponseDto>(guardian);
        }

        public async Task<bool> UpdateAsync(int id, GuardianUpdateDto dto)
        {
            var guardian = await _unitOfWork.Guardians.GetByIdAsync(id);
            if (guardian == null) return false;

            _mapper.Map(dto, guardian);
            await _unitOfWork.Guardians.UpdateAsync(guardian);
            await _unitOfWork.CommitAsync();
            return true;
        }
    }
}
