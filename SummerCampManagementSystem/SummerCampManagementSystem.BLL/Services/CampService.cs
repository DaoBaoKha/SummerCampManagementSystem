using SummerCampManagementSystem.BLL.DTOs.Requests.Camp;
using SummerCampManagementSystem.BLL.DTOs.Responses.Camp;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;

namespace SummerCampManagementSystem.BLL.Services
{
    public class CampService : ICampService
    {
        private readonly IUnitOfWork _unitOfWork;
        public CampService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<CampResponseDto> CreateCampAsync(CampRequestDto camp)
        {
            var newCamp = new Camp
            {
                name = camp.Name,
                description = camp.Description,
                place = camp.Place,
                address = camp.Address,
                startDate = camp.StartDate,
                endDate = camp.EndDate,
                campTypeId = camp.CampTypeId,
                locationId = camp.LocationId,
                minParticipants = camp.MinParticipants,
                maxParticipants = camp.MaxParticipants,
                price = camp.Price,
                status = "Active"
            };

            await _unitOfWork.Camps.CreateAsync(newCamp);
            await _unitOfWork.CommitAsync();

            return new CampResponseDto
            {
                CampId = newCamp.campId,
                Name = newCamp.name,
                Description = newCamp.description,
                Place = newCamp.place,
                Address = newCamp.address,
                StartDate = (DateOnly)newCamp.startDate,
                EndDate = (DateOnly)newCamp.endDate,
                CampTypeId = newCamp.campTypeId,
                LocationId = newCamp.locationId,
                MinParticipants = newCamp.minParticipants ?? 0,
                MaxParticipants = newCamp.maxParticipants ?? 0,
                Price = (decimal)newCamp.price,
                Status = newCamp.status
            };
        }

        public async Task<bool> DeleteCampAsync(int id)
        {
            var existingCamp = await _unitOfWork.Camps.GetByIdAsync(id);
            if (existingCamp == null) return false;
            await _unitOfWork.Camps.RemoveAsync(existingCamp);
            await _unitOfWork.CommitAsync();

            return true;
        }

        public async Task<IEnumerable<Camp>> GetAllCampsAsync()
        {
            return await _unitOfWork.Camps.GetAllAsync();
        }

        public async Task<Camp?> GetCampByIdAsync(int id)
        {
            var camp = await _unitOfWork.Camps.GetByIdAsync(id);
            if(camp == null) return null;
            return camp;
        }

        public async Task<IEnumerable<Camp>> GetCampsByTypeAsync(int campTypeId)
        {
            var camps = await _unitOfWork.Camps.GetCampsByTypeAsync(campTypeId);
            if(camps == null) return Enumerable.Empty<Camp>();
            return camps;
        }

        public async Task<CampResponseDto> UpdateCampAsync(int campId, CampRequestDto camp)
        {
            var existingCamp = await _unitOfWork.Camps.GetByIdAsync(campId);
            if (existingCamp == null) throw new Exception("Camp not found");

            existingCamp.name = camp.Name;
            existingCamp.description = camp.Description;
            existingCamp.place = camp.Place;
            existingCamp.address = camp.Address;
            existingCamp.startDate = camp.StartDate;
            existingCamp.endDate = camp.EndDate;
            existingCamp.campTypeId = camp.CampTypeId;
            existingCamp.locationId = camp.LocationId;
            existingCamp.minParticipants = camp.MinParticipants;
            existingCamp.maxParticipants = camp.MaxParticipants;
            existingCamp.price = camp.Price;
            existingCamp.status = "Active";
            await _unitOfWork.Camps.UpdateAsync(existingCamp);
            await _unitOfWork.CommitAsync();

            return new CampResponseDto
            {
                CampId = existingCamp.campId,
                Name = existingCamp.name,
                Description = existingCamp.description,
                Place = existingCamp.place,
                Address = existingCamp.address,
                StartDate = (DateOnly)existingCamp.startDate,
                EndDate = (DateOnly)existingCamp.endDate,
                CampTypeId = existingCamp.campTypeId,
                MinParticipants = existingCamp.minParticipants ?? 0,
                MaxParticipants = existingCamp.maxParticipants ?? 0,
                LocationId = existingCamp.locationId,
                Price = (decimal)existingCamp.price,
                Status = existingCamp.status
            };

        }
    }
}
