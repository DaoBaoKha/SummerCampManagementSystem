using SummerCampManagementSystem.BLL.DTOs.Requests.CampType;
using SummerCampManagementSystem.BLL.DTOs.Responses.CampType;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;
using SummerCampManagementSystem.DAL.UnitOfWork;

namespace SummerCampManagementSystem.BLL.Services
{
    public class CampTypeService : ICampTypeService
    {
        private readonly ICampTypeRepository _campTypeRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CampTypeService(IUnitOfWork unitOfWork, ICampTypeRepository campTypeRepository)
        {
            _unitOfWork = unitOfWork;
            _campTypeRepository = campTypeRepository;
        }

        public async Task<CampTypeResponseDto> AddCampTypeAsync(CampTypeRequestDto campType)
        {
            var newCampType = new CampType
            {
                name = campType.Name,
                description = campType.Description,
                isActive = true
            };

            await _campTypeRepository.CreateAsync(newCampType);
            await _unitOfWork.CommitAsync();

            return new CampTypeResponseDto
            {
                CampTypeId = newCampType.campTypeId,
                Name = newCampType.name,
                Description = newCampType.description,
                IsActive = true
            };
        }

        public async Task<bool> DeleteCampTypeAsync(int id)
        {
            var existingCampType = await _campTypeRepository.GetCampTypeByIdAsync(id);
            if (existingCampType == null)
            {
                return false;
            }

            await _campTypeRepository.RemoveAsync(existingCampType);
            await _unitOfWork.CommitAsync();

            return true;
        }

        public async Task<IEnumerable<CampType>> GetAllCampTypesAsync()
        {
            return await _campTypeRepository.GetAllAsync();
        }

        public async Task<CampType?> GetCampTypeByIdAsync(int id)
        {
            return await _campTypeRepository.GetCampTypeByIdAsync(id);
        }

        public async Task<CampTypeResponseDto> UpdateCampTypeAsync(int id, CampTypeRequestDto campType)
        {
            var existingCampType = await _campTypeRepository.GetCampTypeByIdAsync(id);

            if (existingCampType == null)
            {
                return null;
            }

            existingCampType.name = campType.Name;
            existingCampType.description = campType.Description;
            await _campTypeRepository.UpdateAsync(existingCampType);
            await _unitOfWork.CommitAsync();

            return new CampTypeResponseDto
            {
                CampTypeId = existingCampType.campTypeId,
                Name = existingCampType.name,
                Description = existingCampType.description,
                IsActive = true
            };
        }
    }
}
