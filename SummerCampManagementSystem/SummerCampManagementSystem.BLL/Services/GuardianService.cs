using AutoMapper;
using SummerCampManagementSystem.BLL.DTOs.Requests.Guardian;
using SummerCampManagementSystem.BLL.DTOs.Responses.Guardian;
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

        public GuardianService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<GuardianResponseDto> CreateAsync(GuardianCreateDto dto)
        {
            var guardian = _mapper.Map<Guardian>(dto);
            await _unitOfWork.Guardians.CreateAsync(guardian);
            await _unitOfWork.CommitAsync();
            return _mapper.Map<GuardianResponseDto>(guardian);
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
