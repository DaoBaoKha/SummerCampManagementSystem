using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.BLL.DTOs.Requests.Registration;
using SummerCampManagementSystem.BLL.DTOs.Responses.Registration;
using SummerCampManagementSystem.BLL.Helpers;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;

namespace SummerCampManagementSystem.BLL.Services
{
    public class RegistrationService : IRegistrationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IValidationService _validationService;

        public RegistrationService(IUnitOfWork unitOfWork, IValidationService validationService)
        {
            _unitOfWork = unitOfWork;
            _validationService = validationService;
        }

        public async Task<RegistrationResponseDto> CreateRegistrationAsync(RegistrationRequestDto request)
        {
            var camp = await _unitOfWork.Camps.GetByIdAsync(request.CampId)
                ?? throw new KeyNotFoundException($"Camp with ID {request.CampId} not found.");

            var newRegistration = new Registration
            {
                campId = request.CampId,
                paymentId = request.PaymentId,
                appliedPromotionId = request.appliedPromotionId,
                registrationCreateAt = DateTime.UtcNow,
                status = "Active"
            };

            foreach (var camperId in request.CamperIds)
            {
                var camper = await _unitOfWork.Campers.GetByIdAsync(camperId)
                    ?? throw new KeyNotFoundException($"Camper with ID {camperId} not found.");

                // attach the camper to avoid duplicate tracking issues
                _unitOfWork.Campers.Attach(camper);

                newRegistration.campers.Add(camper);
            }

            await _unitOfWork.Registrations.CreateAsync(newRegistration);
            await _unitOfWork.CommitAsync();

            return MapToResponseDto(newRegistration, camp.name);
        }

        public async Task<RegistrationResponseDto?> GetRegistrationByIdAsync(int id)
        {
            var registration = await _unitOfWork.Registrations.GetQueryable()
                .Include(r => r.camp)
                .Include(r => r.campers)
                .FirstOrDefaultAsync(r => r.registrationId == id);

            return registration == null ? null : MapToResponseDto(registration, registration.camp.name);
        }

        public async Task<IEnumerable<RegistrationResponseDto>> GetAllRegistrationsAsync()
        {
            var registrations = await _unitOfWork.Registrations.GetQueryable()
                .Include(r => r.camp)
                .Include(r => r.campers)
                .ToListAsync();

            return registrations.Select(r => MapToResponseDto(r, r.camp.name));
        }

        public async Task<RegistrationResponseDto?> UpdateRegistrationAsync(int id, RegistrationRequestDto request)
        {
            var existingRegistration = await _unitOfWork.Registrations.GetQueryable()
                .Include(r => r.campers)
                .FirstOrDefaultAsync(r => r.registrationId == id);

            if (existingRegistration == null) return null;

            await _validationService.ValidateEntityExistsAsync(request.CampId, _unitOfWork.Camps.GetByIdAsync, "Camp");
            existingRegistration.campId = request.CampId;
            existingRegistration.paymentId = request.PaymentId;
            existingRegistration.appliedPromotionId = request.appliedPromotionId;
            existingRegistration.status = "Active";

            var existingCamperIds = existingRegistration.campers.Select(c => c.camperId).ToList();
            var requestedCamperIds = request.CamperIds;

            // find campers to remove
            var campersToRemove = existingRegistration.campers
                .Where(c => !requestedCamperIds.Contains(c.camperId)).ToList();
            foreach (var camper in campersToRemove)
            {
                existingRegistration.campers.Remove(camper);
            }

            // find campers to add
            var camperIdsToAdd = requestedCamperIds
                .Where(camperId => !existingCamperIds.Contains(camperId)).ToList();
            foreach (var camperId in camperIdsToAdd)
            {
                var camperToAdd = await _unitOfWork.Campers.GetByIdAsync(camperId)
                    ?? throw new KeyNotFoundException($"Camper with ID {camperId} not found.");
                existingRegistration.campers.Add(camperToAdd);
            }

            await _unitOfWork.Registrations.UpdateAsync(existingRegistration);
            await _unitOfWork.CommitAsync();

            return await GetRegistrationByIdAsync(id); 
        }

        public async Task<bool> DeleteRegistrationAsync(int id)
        {
            var existingRegistration = await _unitOfWork.Registrations.GetByIdAsync(id);
            if (existingRegistration == null) return false;

            await _unitOfWork.Registrations.RemoveAsync(existingRegistration);
            await _unitOfWork.CommitAsync();

            return true;
        }

        // private helper to map Registration to RegistrationResponseDto
        private RegistrationResponseDto MapToResponseDto(Registration registration, string campName)
        {
            return new RegistrationResponseDto
            {
                registrationId = registration.registrationId,
                CampName = campName,
                PaymentId = registration.paymentId,
                RegistrationCreateAt = (DateTime)registration.registrationCreateAt,
                Status = registration.status,
                Campers = registration.campers.Select(c => new CamperSummaryDto
                {
                    CamperId = c.camperId,
                    CamperName = c.camperName
                }).ToList()
            };
        }
    }
}