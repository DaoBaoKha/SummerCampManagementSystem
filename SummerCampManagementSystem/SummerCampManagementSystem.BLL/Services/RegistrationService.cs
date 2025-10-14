using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore.Query;
using Net.payOS;
using Net.payOS.Types;
using SummerCampManagementSystem.BLL.DTOs.Requests.Registration;
using SummerCampManagementSystem.BLL.DTOs.Responses.Registration;
using SummerCampManagementSystem.BLL.Helpers;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.Core.Enums;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;

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
                registrationCreateAt = DateTime.UtcNow,
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

 
        public async Task<ApproveRegistrationResponseDto> ApproveRegistrationAsync(int registrationId)
        {
            var registration = await _unitOfWork.Registrations.GetQueryable()
                .Include(r => r.camp)
                .Include(r => r.campers)
                .FirstOrDefaultAsync(r => r.registrationId == registrationId);

            if (registration == null) throw new KeyNotFoundException($"Registration with ID {registrationId} not found.");

            if (registration.status != RegistrationStatus.PendingApproval.ToString())
            {
                throw new InvalidOperationException("Only registrations with 'PendingApproval' status can be approved.");
            }

            int amount = (int)registration.camp.price * registration.campers.Count;

            var newPayment = new Payment
            {
                amount = amount,
                paymentDate = DateTime.UtcNow,
                status = "Pending",
                method = "PayOS"
            };

            await _unitOfWork.Payments.CreateAsync(newPayment);
            await _unitOfWork.CommitAsync();

            registration.paymentId = newPayment.paymentId;
            registration.status = RegistrationStatus.PendingPayment.ToString();
            await _unitOfWork.Registrations.UpdateAsync(registration);
            await _unitOfWork.CommitAsync();

            var paymentData = new PaymentData(
                orderCode: newPayment.paymentId,
                amount: amount,
                description: $"Thanh toan don hang #{registration.registrationId}",
                items: new List<ItemData> { new ItemData($"Đăng ký trại hè {registration.camp.name}", registration.campers.Count, amount) },
                cancelUrl: _configuration["PayOS:CancelUrl"],
                returnUrl: _configuration["PayOS:ReturnUrl"]
            );

            CreatePaymentResult createPaymentResult = await _payOS.createPaymentLink(paymentData);

            return new ApproveRegistrationResponseDto
            {
                RegistrationId = registration.registrationId,
                Status = registration.status,
                Amount = amount,
                PaymentUrl = createPaymentResult.checkoutUrl
            };
        }


        public async Task<UpdateRegistrationResponseDto?> UpdateRegistrationAsync(int id, UpdateRegistrationRequestDto request)
        {
            var existingRegistration = await _unitOfWork.Registrations.GetQueryable()
                .Include(r => r.campers)
                .FirstOrDefaultAsync(r => r.registrationId == id);

            if (existingRegistration == null) throw new KeyNotFoundException($"Registration with ID {id} not found.");

            // if confirmed -> no update allow
            if (existingRegistration.status == RegistrationStatus.Confirmed.ToString())
            {
                throw new InvalidOperationException("Cannot update a confirmed registration.");
            }

            // cancel old link and return new link
            if (existingRegistration.paymentId.HasValue)
            {
                try { await _payOS.cancelPaymentLink(existingRegistration.paymentId.Value); }
                catch (Exception ex) { Console.WriteLine($"Could not cancel old payment link (ID: {existingRegistration.paymentId}). Error: {ex.Message}"); }
            }

            // get camper(s)
            var existingCamperIds = existingRegistration.campers.Select(c => c.camperId).ToList();
            var campersToRemove = existingRegistration.campers.Where(c => !request.CamperIds.Contains(c.camperId)).ToList();
            foreach (var camper in campersToRemove) { existingRegistration.campers.Remove(camper); }

            var camperIdsToAdd = request.CamperIds.Where(camperId => !existingCamperIds.Contains(camperId)).ToList();
            foreach (var camperId in camperIdsToAdd)
            {
                var camperToAdd = await _unitOfWork.Campers.GetByIdAsync(camperId) ?? throw new KeyNotFoundException($"Camper with ID {camperId} not found.");
                _unitOfWork.Campers.Attach(camperToAdd);
                existingRegistration.campers.Add(camperToAdd);
            }

            // update info and recalculate money
            var camp = await _unitOfWork.Camps.GetByIdAsync(request.CampId)
                ?? throw new KeyNotFoundException($"Camp with ID {request.CampId} not found.");

            int newAmount = (int)camp.price * request.CamperIds.Count;

            var newPayment = new Payment { amount = newAmount, paymentDate = DateTime.UtcNow, status = "Pending", method = "PayOS" };
            await _unitOfWork.Payments.CreateAsync(newPayment);
            await _unitOfWork.CommitAsync();

            // update regis to change the payment status to pendingPayment
            existingRegistration.campId = request.CampId;
            existingRegistration.appliedPromotionId = request.appliedPromotionId;
            existingRegistration.paymentId = newPayment.paymentId;
            existingRegistration.status = RegistrationStatus.PendingPayment.ToString();

            await _unitOfWork.Registrations.UpdateAsync(existingRegistration);
            await _unitOfWork.CommitAsync();

            // new payos link
            var paymentData = new PaymentData(
                orderCode: newPayment.paymentId,
                amount: newAmount,
                description: $"Cap nhat don hang #{id}",
                items: new List<ItemData> { new ItemData($"Đăng ký trại hè {camp.name}", request.CamperIds.Count, newAmount) },
                cancelUrl: _configuration["PayOS:CancelUrl"],
                returnUrl: _configuration["PayOS:ReturnUrl"]
            );

            CreatePaymentResult newPaymentResult = await _payOS.createPaymentLink(paymentData);

            return new UpdateRegistrationResponseDto
            {
                RegistrationId = id,
                NewAmount = newAmount,
                NewPaymentUrl = newPaymentResult.checkoutUrl
            };
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
    }
}