using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Net.payOS;
using Net.payOS.Types;
using SummerCampManagementSystem.BLL.DTOs.Registration;
using SummerCampManagementSystem.BLL.Helpers;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.Core.Enums;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;
using Transaction = SummerCampManagementSystem.DAL.Models.Transaction;

namespace SummerCampManagementSystem.BLL.Services
{
    public class RegistrationService : IRegistrationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IValidationService _validationService;
        private readonly PayOS _payOS;
        private readonly IConfiguration _configuration;

        public RegistrationService(IUnitOfWork unitOfWork, IValidationService validationService, PayOS payOS, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _validationService = validationService;
            _payOS = payOS;
            _configuration = configuration;
        }

        public async Task<RegistrationResponseDto> CreateRegistrationAsync(CreateRegistrationRequestDto request)
        {
            var camp = await _unitOfWork.Camps.GetByIdAsync(request.CampId)
                ?? throw new KeyNotFoundException($"Camp with ID {request.CampId} not found.");

            var newRegistration = new Registration
            {
                campId = request.CampId,
                appliedPromotionId = request.appliedPromotionId,
                userId = request.userId,
                registrationCreateAt = DateTime.UtcNow,
                note = request.Note,
                status = RegistrationStatus.PendingApproval.ToString()
            };

            foreach (var camperId in request.CamperIds)
            {
                var camper = await _unitOfWork.Campers.GetByIdAsync(camperId)
                    ?? throw new KeyNotFoundException($"Camper with ID {camperId} not found.");
                _unitOfWork.Campers.Attach(camper);
                newRegistration.campers.Add(camper);
            }

            await _unitOfWork.Registrations.CreateAsync(newRegistration);
            await _unitOfWork.CommitAsync();

            newRegistration.camp = camp;
            return MapToResponseDto(newRegistration);
        }

        public async Task<RegistrationResponseDto> ApproveRegistrationAsync(int registrationId)
        {
            var registration = await _unitOfWork.Registrations.GetByIdAsync(registrationId)
                ?? throw new KeyNotFoundException($"Registration with ID {registrationId} not found.");

            if (registration.status != RegistrationStatus.PendingApproval.ToString())
            {
                throw new InvalidOperationException("Only 'PendingApproval' registrations can be approved.");
            }

            registration.status = RegistrationStatus.Approved.ToString();
            await _unitOfWork.Registrations.UpdateAsync(registration);
            await _unitOfWork.CommitAsync();

            return await GetRegistrationByIdAsync(registrationId);
        }

        public async Task<RegistrationResponseDto?> UpdateRegistrationAsync(int id, UpdateRegistrationRequestDto request)
        {
            var existingRegistration = await _unitOfWork.Registrations.GetQueryable()
                .Include(r => r.campers)
                .FirstOrDefaultAsync(r => r.registrationId == id);

            if (existingRegistration == null) throw new KeyNotFoundException($"Registration with ID {id} not found.");

            if (existingRegistration.status == RegistrationStatus.Confirmed.ToString())
            {
                throw new InvalidOperationException("Cannot update a confirmed registration.");
            }

            var wasApprovedOrPendingPayment = existingRegistration.status == RegistrationStatus.Approved.ToString() ||
                                              existingRegistration.status == RegistrationStatus.PendingApproval.ToString() ||
                                              existingRegistration.status == RegistrationStatus.PendingPayment.ToString();

            // update campers
            var campersToRemove = existingRegistration.campers.Where(c => !request.CamperIds.Contains(c.camperId)).ToList();
            foreach (var camper in campersToRemove) { existingRegistration.campers.Remove(camper); }

            var camperIdsToAdd = request.CamperIds.Where(camperId => !existingRegistration.campers.Any(c => c.camperId == camperId)).ToList();
            foreach (var camperId in camperIdsToAdd)
            {
                var camperToAdd = await _unitOfWork.Campers.GetByIdAsync(camperId) ?? throw new KeyNotFoundException($"Camper with ID {camperId} not found.");
                _unitOfWork.Campers.Attach(camperToAdd);
                existingRegistration.campers.Add(camperToAdd);
            }

            var camp = await _unitOfWork.Camps.GetByIdAsync(request.CampId)
                ?? throw new KeyNotFoundException($"Camp with ID {request.CampId} not found.");

            existingRegistration.campId = request.CampId;
            existingRegistration.appliedPromotionId = request.appliedPromotionId;
            existingRegistration.note = request.Note;

            if (wasApprovedOrPendingPayment)
            {
                existingRegistration.status = RegistrationStatus.PendingApproval.ToString();
            }

            await _unitOfWork.Registrations.UpdateAsync(existingRegistration);
            await _unitOfWork.CommitAsync();

            return await GetRegistrationByIdAsync(id);
        }
        public async Task<RegistrationResponseDto?> GetRegistrationByIdAsync(int id)
        {
            var registration = await _unitOfWork.Registrations.GetQueryable()
                .Include(r => r.camp)
                .Include(r => r.campers)
                .FirstOrDefaultAsync(r => r.registrationId == id);
            return registration == null ? null : MapToResponseDto(registration);
        }

        public async Task<IEnumerable<RegistrationResponseDto>> GetAllRegistrationsAsync()
        {
            var registrations = await _unitOfWork.Registrations.GetQueryable()
                .Include(r => r.camp)
                .Include(r => r.campers)
                .ToListAsync();
            return registrations.Select(MapToResponseDto);
        }

        public async Task<bool> DeleteRegistrationAsync(int id)
        {
            var existingRegistration = await _unitOfWork.Registrations.GetByIdAsync(id);
            if (existingRegistration == null) return false;
            await _unitOfWork.Registrations.RemoveAsync(existingRegistration);
            await _unitOfWork.CommitAsync();
            return true;
        }

        private RegistrationResponseDto MapToResponseDto(Registration registration)
        {
            return new RegistrationResponseDto
            {
                registrationId = registration.registrationId,
                CampName = registration.camp?.name ?? "N/A",
                RegistrationCreateAt = (DateTime)registration.registrationCreateAt,
                Status = registration.status,
                Note = registration.note,
                Campers = registration.campers.Select(c => new CamperSummaryDto
                {
                    CamperId = c.camperId,
                    CamperName = c.camperName
                }).ToList()
            };
        }

        public async Task<IEnumerable<RegistrationResponseDto>> GetRegistrationByStatusAsync(RegistrationStatus? status = null)
        {
            IQueryable<Registration> query = _unitOfWork.Registrations.GetQueryable();

            if (status.HasValue)
            {
                string statusString = status.Value.ToString();
                query = query.Where(r => r.status == statusString);
            }

            // use include after where
            var registrations = await query
                .Include(r => r.camp)
                .Include(r => r.campers)
                .ToListAsync();

            return registrations.Select(r => MapToResponseDto(r));
        }

        public async Task<GeneratePaymentLinkResponseDto> GeneratePaymentLinkAsync(int registrationId)
        {
            var registration = await _unitOfWork.Registrations.GetQueryable()
                .Include(r => r.camp)
                .Include(r => r.campers)
                .FirstOrDefaultAsync(r => r.registrationId == registrationId);

            if (registration == null) throw new KeyNotFoundException($"Registration with ID {registrationId} not found.");

            if (registration.status != RegistrationStatus.Approved.ToString())
            {
                throw new InvalidOperationException("Payment link can only be generated for 'Approved' registrations.");
            }

            int amount = (int)registration.camp.price * registration.campers.Count;


            var newTransaction = new Transaction
            {
                amount = amount,
                transactionTime = DateTime.UtcNow,
                status = "Pending", 
                method = "PayOS",
                type = "Payment",
                registrationId = registration.registrationId
            };

            await _unitOfWork.Transactions.CreateAsync(newTransaction);
            await _unitOfWork.CommitAsync();

            // update registration status
            registration.status = RegistrationStatus.PendingPayment.ToString();
            await _unitOfWork.Registrations.UpdateAsync(registration);
            await _unitOfWork.CommitAsync();

            var paymentData = new PaymentData(
                orderCode: registration.registrationId,
                amount: amount,
                description: $"Thanh toan don hang #{registration.registrationId}",
                items: new List<ItemData> { new ItemData($"Đăng ký trại hè {registration.camp.name}", registration.campers.Count, amount) },
                cancelUrl: _configuration["PayOS:CancelUrl"],
                returnUrl: _configuration["PayOS:ReturnUrl"]
            );

            CreatePaymentResult createPaymentResult = await _payOS.createPaymentLink(paymentData);

            return new GeneratePaymentLinkResponseDto
            {
                RegistrationId = registration.registrationId,
                Status = registration.status,
                Amount = amount,
                PaymentUrl = createPaymentResult.checkoutUrl
            };
        }
    }
}