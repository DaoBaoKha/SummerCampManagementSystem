using AutoMapper;
using AutoMapper.QueryableExtensions;
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
        private readonly IMapper _mapper;
        private readonly IUserContextService _userContextService;

        public RegistrationService(IUnitOfWork unitOfWork, IValidationService validationService, 
            PayOS payOS, IConfiguration configuration, IMapper mapper, IUserContextService userContextService)
        {
            _unitOfWork = unitOfWork;
            _validationService = validationService;
            _payOS = payOS;
            _configuration = configuration;
            _mapper = mapper;
            _userContextService = userContextService;
        }

        public async Task<RegistrationResponseDto> CreateRegistrationAsync(CreateRegistrationRequestDto request)
        {
            var camp = await _unitOfWork.Camps.GetByIdAsync(request.CampId)
                ?? throw new KeyNotFoundException($"Camp with ID {request.CampId} not found.");

            var currentUserId = _userContextService.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                throw new UnauthorizedAccessException("Cannot get user ID from token. Please login again.");
            }

            var newRegistration = new Registration
            {
                campId = request.CampId,
                appliedPromotionId = request.appliedPromotionId,
                userId = currentUserId.Value,
                registrationCreateAt = DateTime.UtcNow,
                note = request.Note,
                status = RegistrationStatus.PendingApproval.ToString()
            };

            // add campers
            foreach (var camperId in request.CamperIds)
            {
                var camper = await _unitOfWork.Campers.GetByIdAsync(camperId)
                    ?? throw new KeyNotFoundException($"Camper with ID {camperId} not found.");
                _unitOfWork.Campers.Attach(camper);
                newRegistration.campers.Add(camper);
            }

            await _unitOfWork.Registrations.CreateAsync(newRegistration);
            await _unitOfWork.CommitAsync();

            var createdRegistration = await GetRegistrationByIdAsync(newRegistration.registrationId);
            return createdRegistration;
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
                .AsNoTracking() // add no tracking so EF wont track this
                .FirstOrDefaultAsync(r => r.registrationId == id);

            if (existingRegistration == null) throw new KeyNotFoundException($"Registration with ID {id} not found.");

            if (existingRegistration.status != RegistrationStatus.PendingApproval.ToString() &&
                existingRegistration.status != RegistrationStatus.Approved.ToString())
            {
                throw new InvalidOperationException($"Cannot update registration with status '{existingRegistration.status}'. Only 'PendingApproval' or 'Approved' registrations can be modified.");
            }
            bool requiresReApproval = existingRegistration.status == RegistrationStatus.Approved.ToString();


            _unitOfWork.Registrations.Attach(existingRegistration); //attach this so EF track

            var campersToRemove = existingRegistration.campers
                .Where(c => !request.CamperIds.Contains(c.camperId)).ToList();
            foreach (var camper in campersToRemove)
            {
                existingRegistration.campers.Remove(camper); 
            }

            var camperIdsToAdd = request.CamperIds
                .Where(camperId => !existingRegistration.campers.Any(c => c.camperId == camperId)).ToList();
            foreach (var camperId in camperIdsToAdd)
            {
                var camperToAdd = await _unitOfWork.Campers.GetByIdAsync(camperId)
                    ?? throw new KeyNotFoundException($"Camper with ID {camperId} not found.");

                _unitOfWork.Campers.Attach(camperToAdd);

                existingRegistration.campers.Add(camperToAdd); 
            }

            var camp = await _unitOfWork.Camps.GetByIdAsync(request.CampId)
                ?? throw new KeyNotFoundException($"Camp with ID {request.CampId} not found.");

            existingRegistration.campId = request.CampId;
            existingRegistration.appliedPromotionId = request.appliedPromotionId;
            existingRegistration.note = request.Note;

            if (requiresReApproval)
            {
                existingRegistration.status = RegistrationStatus.PendingApproval.ToString();
            }

            await _unitOfWork.CommitAsync();

            return await GetRegistrationByIdAsync(id);
        }
        public async Task<RegistrationResponseDto?> GetRegistrationByIdAsync(int id)
        {
            var registration = await _unitOfWork.Registrations.GetQueryable()
                .Where(r => r.registrationId == id)
                .ProjectTo<RegistrationResponseDto>(_mapper.ConfigurationProvider) 
                .FirstOrDefaultAsync();

            return registration;
        }

        public async Task<IEnumerable<RegistrationResponseDto>> GetAllRegistrationsAsync()
        {
            var registrations = await _unitOfWork.Registrations.GetQueryable()
                .ProjectTo<RegistrationResponseDto>(_mapper.ConfigurationProvider) 
                .ToListAsync();

            return registrations;
        }

        public async Task<bool> DeleteRegistrationAsync(int id)
        {
            var existingRegistration = await _unitOfWork.Registrations.GetByIdAsync(id);
            if (existingRegistration == null) return false;
            await _unitOfWork.Registrations.RemoveAsync(existingRegistration);
            await _unitOfWork.CommitAsync();
            return true;
        }

        public async Task<IEnumerable<RegistrationResponseDto>> GetRegistrationByStatusAsync(RegistrationStatus? status = null)
        {
            IQueryable<Registration> query = _unitOfWork.Registrations.GetQueryable();
            if (status.HasValue)
            {
                string statusString = status.Value.ToString();
                query = query.Where(r => r.status == statusString);
            }

            var registrations = await query
                .ProjectTo<RegistrationResponseDto>(_mapper.ConfigurationProvider) 
                .ToListAsync();

            return registrations;
        }

        public async Task<GeneratePaymentLinkResponseDto> GeneratePaymentLinkAsync(int registrationId, GeneratePaymentLinkRequestDto request)
        {
            //  load registration
            var registration = await _unitOfWork.Registrations.GetQueryable()
                .Include(r => r.camp)
                .Include(r => r.campers)
                .Include(r => r.appliedPromotion)
                .Include(r => r.RegistrationOptionalActivities)
                .FirstOrDefaultAsync(r => r.registrationId == registrationId); 

            if (registration == null) throw new KeyNotFoundException($"Registration with ID {registrationId} not found.");

            // check registration status
            if (registration.status != RegistrationStatus.Approved.ToString())
            {
                throw new InvalidOperationException("Payment link can only be generated for 'Approved' registrations.");
            }

            // check transaction status
            var existingPendingTransaction = await _unitOfWork.Transactions.GetQueryable()
                .Where(t => t.registrationId == registrationId && t.status == "Pending")
                .OrderByDescending(t => t.transactionTime)
                .FirstOrDefaultAsync();

            // if theres old payment link -> return old link
            if (existingPendingTransaction != null)
            {
                return new GeneratePaymentLinkResponseDto
                {
                    RegistrationId = registration.registrationId,
                    Status = registration.status,
                    Amount = (decimal)existingPendingTransaction.amount,
                    PaymentUrl = $"{_configuration["PayOS:RedirectUrl"]}?orderCode={existingPendingTransaction.transactionCode}"
                };
            }

            // start using dbcontext
            var dbContext = (CampEaseDatabaseContext)_unitOfWork.GetDbContext();

            
            using (var transaction = await dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    int optionalActivitiesTotalCost = 0; 

                    // remove old regis optional if theres any
                    if (registration.RegistrationOptionalActivities != null && registration.RegistrationOptionalActivities.Any())
                    {
                        _unitOfWork.RegistrationOptionalActivities.RemoveRange(registration.RegistrationOptionalActivities);
                    }

                    var schedulesToUpdate = new Dictionary<int, int>();

                    if (request.OptionalChoices != null && request.OptionalChoices.Any())
                    {
                        var activityScheduleIds = request.OptionalChoices.Select(oc => oc.ActivityScheduleId).Distinct().ToList();

                        // load ActivitySchedules
                        var activitySchedules = await _unitOfWork.ActivitySchedules.GetQueryable()
                            .Where(s => activityScheduleIds.Contains(s.activityScheduleId))
                            .ToListAsync();

                        foreach (var optionalChoice in request.OptionalChoices)
                        {
                            var schedule = activitySchedules.FirstOrDefault(s => s.activityScheduleId == optionalChoice.ActivityScheduleId)
                                ?? throw new KeyNotFoundException($"Activity schedule with ID {optionalChoice.ActivityScheduleId} not found.");

                            // validation
                            if (!schedule.isOptional)
                                throw new InvalidOperationException($"Activity schedule with ID {optionalChoice.ActivityScheduleId} is not an optional activity.");
                            if (!registration.campers.Any(c => c.camperId == optionalChoice.CamperId))
                                throw new InvalidOperationException($"Camper with ID {optionalChoice.CamperId} is not part of this registration.");

                            // create new regisOptional
                            var optionalReg = new RegistrationOptionalActivity
                            {
                                registrationId = registration.registrationId,
                                camperId = optionalChoice.CamperId,
                                activityScheduleId = optionalChoice.ActivityScheduleId,
                                status = "Holding", 
                                createdTime = DateTime.UtcNow
                            };
                            _unitOfWork.RegistrationOptionalActivities.CreateAsync(optionalReg);

                            // update to increase current capacity
                            schedulesToUpdate[schedule.activityScheduleId] = schedulesToUpdate.GetValueOrDefault(schedule.activityScheduleId) + 1;
                        }

                        // check current capacity and update
                        foreach (var (scheduleId, count) in schedulesToUpdate)
                        {
                            var schedule = activitySchedules.First(s => s.activityScheduleId == scheduleId);
                            if ((schedule.currentCapacity ?? 0) + count > schedule.maxCapacity)
                            {
                                throw new InvalidOperationException($"Activity schedule {scheduleId} has insufficient capacity. Requested: {count}, Available: {schedule.maxCapacity - (schedule.currentCapacity ?? 0)}");
                            }

                            schedule.currentCapacity = (schedule.currentCapacity ?? 0) + count;
                            _unitOfWork.ActivitySchedules.UpdateAsync(schedule);
                        }
                    }

                    // price logic
                    int baseAmount = (int)registration.camp.price * registration.campers.Count;
                    int finalAmount = baseAmount + optionalActivitiesTotalCost; 

                    // promotion logic
                    if (registration.appliedPromotionId.HasValue && registration.appliedPromotion != null)
                    {
                        var promotion = registration.appliedPromotion;
                        decimal discount = 0;

                        if (promotion.status == "Active" &&
                            (!promotion.startDate.HasValue || promotion.startDate.Value.ToDateTime(TimeOnly.MinValue) <= DateTime.UtcNow) &&
                            (!promotion.endDate.HasValue || promotion.endDate.Value.ToDateTime(TimeOnly.MinValue) >= DateTime.UtcNow))
                        {
                            if (promotion.percent.HasValue)
                                discount = (decimal)baseAmount * (promotion.percent.Value / 100);

                            if (promotion.maxDiscountAmount.HasValue && discount > promotion.maxDiscountAmount.Value)
                                discount = promotion.maxDiscountAmount.Value;
                        }
                        finalAmount = (int)(finalAmount - discount);
                        if (finalAmount < 0) finalAmount = 0;
                    }

                    // create transaction
                    var newTransaction = new Transaction
                    {
                        amount = finalAmount,
                        transactionTime = DateTime.UtcNow,
                        status = "Pending",
                        method = "PayOS",
                        type = "Payment",
                        registrationId = registration.registrationId
                    };
                    await _unitOfWork.Transactions.CreateAsync(newTransaction);

                    await _unitOfWork.CommitAsync();

                    // update transactionCode after getting the transactionId
                    long uniqueOrderCode = long.Parse($"{newTransaction.transactionId}{DateTime.Now:fff}");
                    newTransaction.transactionCode = uniqueOrderCode.ToString();
                    await _unitOfWork.Transactions.UpdateAsync(newTransaction);

                    registration.status = RegistrationStatus.PendingPayment.ToString();
                    await _unitOfWork.Registrations.UpdateAsync(registration);

                    await _unitOfWork.CommitAsync();

                    // create payment link
                    var paymentData = new PaymentData(
                        orderCode: uniqueOrderCode,
                        amount: finalAmount,
                        description: $"Thanh toan don hang #{registration.registrationId}",
                        items: new List<ItemData> {
                    new ItemData($"Đăng ký trại hè {registration.camp.name}", registration.campers.Count, baseAmount)
                        },
                        cancelUrl: _configuration["PayOS:CancelUrl"],
                        returnUrl: _configuration["PayOS:ReturnUrl"]
                    );

                    CreatePaymentResult createPaymentResult = await _payOS.createPaymentLink(paymentData);

                    await transaction.CommitAsync();

                    return new GeneratePaymentLinkResponseDto
                    {
                        RegistrationId = registration.registrationId,
                        Status = registration.status,
                        Amount = finalAmount,
                        PaymentUrl = createPaymentResult.checkoutUrl
                    };
                }
                catch (Exception)
                {
                    // rollback if theres any error at any step
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }
    }
}