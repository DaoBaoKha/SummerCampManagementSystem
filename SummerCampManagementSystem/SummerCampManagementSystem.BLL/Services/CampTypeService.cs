using SummerCampManagementSystem.BLL.DTOs.Requests.CampType;
using SummerCampManagementSystem.BLL.DTOs.Responses.CampType;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;

namespace SummerCampManagementSystem.BLL.Services
{
    public class CampTypeService : ICampTypeService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CampTypeService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<CampTypeResponseDto> AddCampTypeAsync(CampTypeRequestDto campType)
        {
            var newCampType = new CampType
            {
                name = campType.Name,
                description = campType.Description,
                isActive = true
            };

            await _unitOfWork.CampTypes.CreateAsync(newCampType);
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
            var existingCampType = await _unitOfWork.CampTypes.GetCampTypeByIdAsync(id);
            if (existingCampType == null)
            {
                return false;
            }

            await _unitOfWork.CampTypes.RemoveAsync(existingCampType);
            await _unitOfWork.CommitAsync();

            return true;
        }

        public async Task<IEnumerable<CampType>> GetAllCampTypesAsync()
        {
            return await _unitOfWork.CampTypes.GetAllAsync();
        }

        public async Task<CampType?> GetCampTypeByIdAsync(int id)
        {
            return await _unitOfWork.CampTypes.GetCampTypeByIdAsync(id);
        }

        public async Task<CampTypeResponseDto> UpdateCampTypeAsync(int id, CampTypeRequestDto campType)
        {
            var existingCampType = await _unitOfWork.CampTypes.GetCampTypeByIdAsync(id);

            if (existingCampType == null)
            {
                return null;
            }

            existingCampType.name = campType.Name;
            existingCampType.description = campType.Description;
            await _unitOfWork.CampTypes.UpdateAsync(existingCampType);
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
