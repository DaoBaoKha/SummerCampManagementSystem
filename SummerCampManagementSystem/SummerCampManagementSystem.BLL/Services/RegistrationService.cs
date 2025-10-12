using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Net.payOS;
using Net.payOS.Types;
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
        private readonly PayOS _payOS;
        private readonly IConfiguration _configuration;

        public RegistrationService(IUnitOfWork unitOfWork, IValidationService validationService,
            PayOS payOS, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _validationService = validationService;
            _payOS = payOS;
            _configuration = configuration;
        }

        public async Task<CreateRegistrationResponseDto> CreateRegistrationAsync(CreateRegistrationRequestDto request)
        {

            var camp = await _unitOfWork.Camps.GetByIdAsync(request.CampId)
                ?? throw new KeyNotFoundException($"Camp with ID {request.CampId} not found.");

            // calculate total amount
            int amount = (int)camp.price * request.CamperIds.Count;

            // new payment with pending status
            var newPayment = new Payment
            {
                amount = amount,
                paymentDate = DateTime.UtcNow,
                status = "Pending",
                method = "PayOS"
            };

            await _unitOfWork.Payments.CreateAsync(newPayment);
            await _unitOfWork.CommitAsync(); //take paymentId from here


            // new regis with pending payment status
            var newRegistration = new Registration
            {
                campId = request.CampId,
                paymentId = newPayment.paymentId, //take paymentId from here
                appliedPromotionId = request.appliedPromotionId,
                registrationCreateAt = DateTime.UtcNow,
                status = "PendingPayment"
            };

            foreach (var camperId in request.CamperIds)
            {
                var camper = await _unitOfWork.Campers.GetByIdAsync(camperId)
                    ?? throw new KeyNotFoundException($"Camper with ID {camperId} not found.");


                //attach camper to context to avoid duplicate entity error
                _unitOfWork.Campers.Attach(camper);

                newRegistration.campers.Add(camper);
            }

            await _unitOfWork.Registrations.CreateAsync(newRegistration);
            await _unitOfWork.CommitAsync();


            // create payment link with payOS
            var item = new ItemData($"Đăng ký trại hè {camp.name}", request.CamperIds.Count, amount);
            var items = new List<ItemData> { item };

            var paymentData = new PaymentData(
                orderCode: newPayment.paymentId,
                amount: amount,
                description: $"Thanh toán đơn hàng#{newRegistration.registrationId}",
                items: items,
                cancelUrl: "https://example.com/payment-cancelled",
                returnUrl: "https://example.com/payment-success"
            );

            CreatePaymentResult createPaymentResult = await _payOS.createPaymentLink(paymentData);

            // return url for client
            return new CreateRegistrationResponseDto
            {
                RegistrationId = newRegistration.registrationId,
                Status = newRegistration.status,
                Amount = (decimal)newPayment.amount,
                PaymentUrl = createPaymentResult.checkoutUrl
            };
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

        public async Task<UpdateRegistrationResponseDto?> UpdateRegistrationAsync(int id, UpdateRegistrationRequestDto request)
        {
            var existingRegistration = await _unitOfWork.Registrations.GetQueryable()
                .Include(r => r.campers)
                .FirstOrDefaultAsync(r => r.registrationId == id);

            if (existingRegistration == null)
            {
                throw new KeyNotFoundException($"Registration with ID {id} not found.");
            }

            // only allow updates if status is "PendingPayment"
            if (existingRegistration.status != "PendingPayment")
            {
                throw new InvalidOperationException("Only registrations with 'PendingPayment' status can be updated.");
            }

            // cancel old payment link if exists
            if (existingRegistration.paymentId.HasValue)
            {
                try
                {
                    await _payOS.cancelPaymentLink(existingRegistration.paymentId.Value);
                }
                catch (Exception ex)
                {
                    // log error but continue
                    Console.WriteLine($"Could not cancel old payment link (ID: {existingRegistration.paymentId}). Error: {ex.Message}");
                }
            }

            
            var existingCamperIds = existingRegistration.campers.Select(c => c.camperId).ToList();
            var campersToRemove = existingRegistration.campers.Where(c => !request.CamperIds.Contains(c.camperId)).ToList();
            foreach (var camper in campersToRemove)
            {
                existingRegistration.campers.Remove(camper);
            }


            var camperIdsToAdd = request.CamperIds.Where(camperId => !existingCamperIds.Contains(camperId)).ToList();

            foreach (var camperId in camperIdsToAdd)
            {
                var camperToAdd = await _unitOfWork.Campers.GetByIdAsync(camperId)
                    ?? throw new KeyNotFoundException($"Camper with ID {camperId} not found.");

                _unitOfWork.Campers.Attach(camperToAdd); 
                existingRegistration.campers.Add(camperToAdd);
            }


            // calculate new amount
            var camp = await _unitOfWork.Camps.GetByIdAsync(request.CampId)
                ?? throw new KeyNotFoundException($"Camp with ID {request.CampId} not found.");

            int newAmount = (int)camp.price * request.CamperIds.Count;

            var newPayment = new Payment
            {
                amount = newAmount,
                paymentDate = DateTime.UtcNow,
                status = "Pending",
                method = "PayOS"
            };
            await _unitOfWork.Payments.CreateAsync(newPayment);
            await _unitOfWork.CommitAsync();

            // update registration
            existingRegistration.campId = request.CampId;
            existingRegistration.appliedPromotionId = request.appliedPromotionId;
            existingRegistration.paymentId = newPayment.paymentId; // new paymentId

            await _unitOfWork.Registrations.UpdateAsync(existingRegistration);
            await _unitOfWork.CommitAsync();

            // create new payment link with payOS
            var paymentData = new PaymentData(
                orderCode: newPayment.paymentId, // use new paymentId
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