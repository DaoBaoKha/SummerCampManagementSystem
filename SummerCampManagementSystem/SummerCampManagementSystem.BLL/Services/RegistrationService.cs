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

        public async Task<RegistrationResponseDto> CreateRegistrationAsync(RegistrationRequestDto registration)
        {
            await _validationService.ValidateEntityExistsAsync((int)registration.CamperId, _unitOfWork.CamperGroups.GetByIdAsync, "Camper");
            await _validationService.ValidateEntityExistsAsync((int)registration.CampId, _unitOfWork.Camps.GetByIdAsync, "Camp");
            //await _validationService.ValidateEntityExistsAsync(registration.PaymentId, _unitOfWork.Payments.GetByIdAsync, "Payment");
            //await _validationService.ValidateEntityExistsAsync(registration.AppliedPromotionId, _unitOfWork.Promotions.GetByIdAsync, "Promotion", true);

            var newRegistration = new Registration
            {
                camperId = registration.CamperId,
                campId = registration.CampId,
                paymentId = registration.PaymentId,
                registrationCreateAt = DateTime.UtcNow,
                status = "Active",
                appliedPromotionId = registration.appliedPromotionId
            };

            await _unitOfWork.Registrations.CreateAsync(newRegistration);
            await _unitOfWork.CommitAsync();

            return new RegistrationResponseDto
            {
                registrationId = newRegistration.registrationId,
                CamperId = (int)newRegistration.camperId,
                CampId = (int)newRegistration.campId,
                PaymentId = (int)newRegistration.paymentId,
                appliedPromotionId = newRegistration.appliedPromotionId,
                RegistrationCreateAt = (DateTime)newRegistration.registrationCreateAt,
                Status = newRegistration.status
            };
        }

        public async Task<bool> DeleteRegistrationAsync(int id)
        {
            var existingRegistration = await _unitOfWork.Registrations.GetByIdAsync(id);
            if (existingRegistration == null) return false;

            await _unitOfWork.Registrations.RemoveAsync(existingRegistration);
            await _unitOfWork.CommitAsync();

            return true;
        }

        public async Task<IEnumerable<RegistrationResponseDto>> GetAllRegistrationsAsync()
        {
            var registrations = await _unitOfWork.Registrations.GetAllAsync();

            return registrations.Select(r => new RegistrationResponseDto
            {
                registrationId = r.registrationId,
                CamperId = (int)r.camperId,
                CampId = (int)r.campId,
                PaymentId = (int)r.paymentId,
                appliedPromotionId = r.appliedPromotionId,
                RegistrationCreateAt = (DateTime)r.registrationCreateAt,
                Status = r.status
            });
        }

        public async Task<RegistrationResponseDto?> GetRegistrationByIdAsync(int id)
        {
            var registration = await _unitOfWork.Registrations.GetByIdAsync(id);
            if (registration == null) return null;

            return new RegistrationResponseDto
            {
                registrationId = registration.registrationId,
                CamperId = (int)registration.camperId,
                CampId = (int)registration.campId,
                PaymentId = (int)registration.paymentId,
                appliedPromotionId = registration.appliedPromotionId,
                RegistrationCreateAt = (DateTime)registration.registrationCreateAt,
                Status = registration.status
            };
        }

        public async Task<RegistrationResponseDto?> UpdateRegistrationAsync(int id, RegistrationRequestDto registration)
        {
            await _validationService.ValidateEntityExistsAsync((int)registration.CamperId, _unitOfWork.CamperGroups.GetByIdAsync, "Camper");
            await _validationService.ValidateEntityExistsAsync((int)registration.CampId, _unitOfWork.Camps.GetByIdAsync, "Camp");
            //await _validationService.ValidateEntityExistsAsync(registration.PaymentId, _unitOfWork.Payments.GetByIdAsync, "Payment");
            //await _validationService.ValidateEntityExistsAsync(registration.AppliedPromotionId, _unitOfWork.Promotions.GetByIdAsync, "Promotion", true);

            var existingRegistration = await _unitOfWork.Registrations.GetByIdAsync(id);
            if (existingRegistration == null) return null;

            existingRegistration.camperId = registration.CamperId;
            existingRegistration.campId = registration.CampId;
            existingRegistration.paymentId = registration.PaymentId;
            existingRegistration.appliedPromotionId = registration.appliedPromotionId;
            existingRegistration.status = "Active";

            //not updating registrationCreateAt to preserve original creation time
            await _unitOfWork.Registrations.UpdateAsync(existingRegistration);
            await _unitOfWork.CommitAsync();

            return new RegistrationResponseDto
            {
                registrationId = existingRegistration.registrationId,
                CamperId = (int)existingRegistration.camperId,
                CampId = (int)existingRegistration.campId,
                PaymentId = (int)existingRegistration.paymentId,
                appliedPromotionId = existingRegistration.appliedPromotionId,
                RegistrationCreateAt = (DateTime)existingRegistration.registrationCreateAt,
                Status = existingRegistration.status
            };
        }
    }
}
