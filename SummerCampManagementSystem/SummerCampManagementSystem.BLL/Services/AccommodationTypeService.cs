using AutoMapper;
using SummerCampManagementSystem.BLL.DTOs.AccommodationType;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;

namespace SummerCampManagementSystem.BLL.Services
{
    public class AccommodationTypeService : IAccommodationTypeService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public AccommodationTypeService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<AccommodationTypeResponseDto> CreateAsync(AccommodationTypeRequestDto accommodationTypeRequestDto)
        {
            var accommodationType = _mapper.Map<AccommodationType>(accommodationTypeRequestDto);

            accommodationType.isActive = true;

            await _unitOfWork.AccommodationTypes.CreateAsync(accommodationType);
            await _unitOfWork.CommitAsync();

            return _mapper.Map<AccommodationTypeResponseDto>(accommodationType);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var accommodationType = await _unitOfWork.AccommodationTypes.GetByIdAsync(id);

            if (accommodationType == null)
            {
                throw new KeyNotFoundException("Accommodation Type not found");
            }

            accommodationType.isActive = false;
            await _unitOfWork.AccommodationTypes.UpdateAsync(accommodationType);
            await _unitOfWork.CommitAsync();

            return true;

        }

        public async Task<IEnumerable<AccommodationTypeResponseDto>> GetAllAsync()
        {
            var accommodationTypes = await _unitOfWork.AccommodationTypes.GetAllAsync();

            return _mapper.Map<IEnumerable<AccommodationTypeResponseDto>>(accommodationTypes);
        }

        public async Task<AccommodationTypeResponseDto?> GetByIdAsync(int id)
        {
            var accommodationType = await _unitOfWork.AccommodationTypes.GetByIdAsync(id);

            if (accommodationType == null)
            {
                throw new KeyNotFoundException("Accommodation Type not found");
            }

            return _mapper.Map<AccommodationTypeResponseDto>(accommodationType);
        }

        public async Task<AccommodationTypeResponseDto?> UpdateAsync(int id, AccommodationTypeRequestDto accommodationTypeRequestDto)
        {
            var accommodationType = await _unitOfWork.AccommodationTypes.GetByIdAsync(id);
            if (accommodationType == null)
            {
                throw new KeyNotFoundException("Accommodation Type not found");
            }

            _mapper.Map(accommodationTypeRequestDto, accommodationType);
            await _unitOfWork.AccommodationTypes.UpdateAsync(accommodationType);
            await _unitOfWork.CommitAsync();

            return _mapper.Map<AccommodationTypeResponseDto>(accommodationType);
        }
    }
}
