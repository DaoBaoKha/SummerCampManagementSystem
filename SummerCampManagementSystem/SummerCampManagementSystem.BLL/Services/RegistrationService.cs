using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
        private readonly IPayOSService _payOSService;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;
        private readonly IUserContextService _userContextService;

        public RegistrationService(IUnitOfWork unitOfWork, IValidationService validationService,
            IPayOSService payOSService, IConfiguration configuration, IMapper mapper, IUserContextService userContextService)
        {
            _unitOfWork = unitOfWork;
            _validationService = validationService;
            _payOSService = payOSService;
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

            // use private method to check
            foreach (var camperId in request.CamperIds)
            {
                await ValidateCamperNotAlreadyRegisteredAsync(request.CampId, camperId);
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

            // ADD CAMPERS (Using RegistrationCamper entity for explicit M-to-M)
            newRegistration.RegistrationCampers = new List<RegistrationCamper>();
            foreach (var camperId in request.CamperIds)
            {
                // check if camper exists 
                var camper = await _unitOfWork.Campers.GetByIdAsync(camperId)
                    ?? throw new KeyNotFoundException($"Camper with ID {camperId} not found.");

                // create
                var registrationCamper = new RegistrationCamper
                {
                    camperId = camperId,

                    /*
                     * STATUS REGISTRATION & REGISTRATION CAMPER = PENDING APPROVAL
                     */
                    status = RegistrationCamperStatus.PendingApproval.ToString() 
                };
                newRegistration.RegistrationCampers.Add(registrationCamper);
            }

            await _unitOfWork.Registrations.CreateAsync(newRegistration);
            await _unitOfWork.CommitAsync();

            // Load the created registration entity with includes for response
            var createdRegistration = await GetRegistrationByIdAsync(newRegistration.registrationId);
            return createdRegistration;
        }

        public async Task<RegistrationResponseDto> ApproveRegistrationAsync(int registrationId)
        {
            // load Registration with related RegistrationCampers 
            var registration = await _unitOfWork.Registrations.GetQueryable()
        .Include(r => r.RegistrationCampers)
        .FirstOrDefaultAsync(r => r.registrationId == registrationId)
        ?? throw new KeyNotFoundException($"Registration with ID {registrationId} not found.");

            if (registration.status != RegistrationStatus.PendingApproval.ToString())
            {
                throw new InvalidOperationException("Only 'PendingApproval' registrations can be approved.");
            }

            /*
             * STATUS REGISTRATION = APPROVED
             * STATUS REGISTRATIONCAMPER = REGISTERED
             */
            registration.status = RegistrationStatus.Approved.ToString();
            await _unitOfWork.Registrations.UpdateAsync(registration);

            // status of each Camper -> Registered
            foreach (var camperLink in registration.RegistrationCampers)
            {
                if (camperLink.status == RegistrationCamperStatus.PendingApproval.ToString())
                {
                    camperLink.status = RegistrationCamperStatus.Approved.ToString();
                    await _unitOfWork.RegistrationCampers.UpdateAsync(camperLink);
                }
            }

            await _unitOfWork.CommitAsync();

            return await GetRegistrationByIdAsync(registrationId);
        }

        public async Task<RegistrationResponseDto?> UpdateRegistrationAsync(int id, UpdateRegistrationRequestDto request)
        {
            var existingRegistration = await _unitOfWork.Registrations.GetQueryable()
                .Include(r => r.RegistrationCampers) // Include link table
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.registrationId == id);

            if (existingRegistration == null) throw new KeyNotFoundException($"Registration with ID {id} not found.");

            if (existingRegistration.status != RegistrationStatus.PendingApproval.ToString() &&
                existingRegistration.status != RegistrationStatus.Approved.ToString())
            {
                throw new InvalidOperationException($"Cannot update registration with status '{existingRegistration.status}'. Only 'PendingApproval' or 'Approved' registrations can be modified.");
            }
            bool requiresReApproval = existingRegistration.status == RegistrationStatus.Approved.ToString();

            // Access DbContext directly for complex M-to-M entity tracking
            var dbContext = (CampEaseDatabaseContext)_unitOfWork.GetDbContext();

            // get existing link
            var oldCamperLinks = await dbContext.RegistrationCampers
                .Where(rc => rc.registrationId == id)
                .ToListAsync();

            // remove links for Campers excluded from the updated list
            var linksToRemove = oldCamperLinks
                .Where(rc => !request.CamperIds.Contains(rc.camperId)).ToList();

            dbContext.RegistrationCampers.RemoveRange(linksToRemove);

            // add links for new Campers
            var existingCamperIds = oldCamperLinks.Select(rc => rc.camperId).ToList();
            var camperIdsToAdd = request.CamperIds
                .Where(camperId => !existingCamperIds.Contains(camperId)).ToList();

            foreach (var camperId in camperIdsToAdd)
            {
                // use private to check campers
                await ValidateCamperNotAlreadyRegisteredAsync(request.CampId, camperId);

                // Create new link entity
                var newLink = new RegistrationCamper
                {
                    registrationId = id,
                    camperId = camperId,
                    // Status is Pending if it was Approved before, ensuring re-approval if changes are made
                    status = requiresReApproval ? "Pending" : "Pending"
                };
                dbContext.RegistrationCampers.Add(newLink);
            }

            // update main Registration entity
            dbContext.Registrations.Attach(existingRegistration);

            var camp = await _unitOfWork.Camps.GetByIdAsync(request.CampId)
                ?? throw new KeyNotFoundException($"Camp with ID {request.CampId} not found.");

            existingRegistration.campId = request.CampId;
            existingRegistration.appliedPromotionId = request.appliedPromotionId;
            existingRegistration.note = request.Note;

            if (requiresReApproval)
            {
                // if it already approved, return to pending approval status
                existingRegistration.status = RegistrationStatus.PendingApproval.ToString();
            }

            await _unitOfWork.CommitAsync();

            return await GetRegistrationByIdAsync(id);
        }
        public async Task<RegistrationResponseDto?> GetRegistrationByIdAsync(int id)
        {
            var registrationEntity = await _unitOfWork.Registrations.GetQueryable()
                .Where(r => r.registrationId == id)
                .Include(r => r.camp)
                // include through the RegistrationCampers junction table
                .Include(r => r.RegistrationCampers).ThenInclude(rc => rc.camper)
                .Include(r => r.appliedPromotion)
                .Include(r => r.RegistrationOptionalActivities)
                .FirstOrDefaultAsync();

            if (registrationEntity == null) return null;

            // Map data to DTO (AutoMapper will use the updated profile to extract campers from RegistrationCampers)
            var responseDto = _mapper.Map<RegistrationResponseDto>(registrationEntity);

            // Add final price
            responseDto.FinalPrice = CalculateFinalPrice(registrationEntity);

            return responseDto;
        }


        public async Task<IEnumerable<RegistrationResponseDto>> GetAllRegistrationsAsync()
        {
            var registrationEntities = await _unitOfWork.Registrations.GetQueryable()
                .Include(r => r.camp)
                // include through the RegistrationCampers junction table
                .Include(r => r.RegistrationCampers).ThenInclude(rc => rc.camper)
                .Include(r => r.appliedPromotion)
                .Include(r => r.RegistrationOptionalActivities)
                .ToListAsync();

            var responseDtos = _mapper.Map<IEnumerable<RegistrationResponseDto>>(registrationEntities).ToList();

            // Get final price for each registration
            for (int i = 0; i < registrationEntities.Count; i++)
            {
                responseDtos[i].FinalPrice = CalculateFinalPrice(registrationEntities[i]);
            }

            return responseDtos;
        }

        public async Task<bool> DeleteRegistrationAsync(int registrationId)
        {
            // validation
            var currentUserId = _userContextService.GetCurrentUserId();
            if (currentUserId == null)
            {
                throw new UnauthorizedAccessException("User is not authenticated.");
            }

            var registration = await _unitOfWork.Registrations.GetByIdAsync(registrationId);
            if (registration == null)
            {
                throw new KeyNotFoundException($"Registration with ID {registrationId} not found.");
            }

            if (registration.userId != currentUserId.Value)
            {
                throw new UnauthorizedAccessException("You do not have permission to cancel this registration.");
            }

            // only allow cancellation if status is NOT: Confirmed, PendingRefund, Refunded, Canceled
            if (registration.status == RegistrationStatus.Confirmed.ToString() ||
                registration.status == RegistrationStatus.PendingRefund.ToString() ||
                registration.status == RegistrationStatus.Refunded.ToString() ||
                registration.status == RegistrationStatus.Canceled.ToString()) 
            {
                throw new InvalidOperationException($"Cannot cancel registration with status '{registration.status}'.");
            }

            // soft delete 
            registration.status = RegistrationStatus.Canceled.ToString();

            await _unitOfWork.Registrations.UpdateAsync(registration);
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

            var registrationEntities = await query
                .Include(r => r.camp)
                // FIX: Include through the RegistrationCampers junction table
                .Include(r => r.RegistrationCampers).ThenInclude(rc => rc.camper)
                .Include(r => r.appliedPromotion)
                .ToListAsync();

            var responseDtos = _mapper.Map<IEnumerable<RegistrationResponseDto>>(registrationEntities).ToList();

            // Get final price for each registration
            for (int i = 0; i < registrationEntities.Count; i++)
            {
                responseDtos[i].FinalPrice = CalculateFinalPrice(registrationEntities[i]);
            }

            return responseDtos;
        }

        public async Task<GeneratePaymentLinkResponseDto> GeneratePaymentLinkAsync(int registrationId, GeneratePaymentLinkRequestDto request, bool isMobile)
        {
            // load registration
            var registration = await GetRegistrationForPaymentAsync(registrationId);

            if (registration == null) throw new KeyNotFoundException($"Registration with ID {registrationId} not found.");

            // validate Status
            if (registration.status != RegistrationStatus.Approved.ToString() &&
                registration.status != RegistrationStatus.PendingPayment.ToString())
            {
                throw new InvalidOperationException("Payment link can only be generated for 'Approved' or 'PendingPayment' registrations.");
            }

            // validate Campers
            if (!registration.RegistrationCampers.Any() ||
                !registration.RegistrationCampers.All(rc => rc.status == RegistrationCamperStatus.Approved.ToString()))
            {
                throw new InvalidOperationException("All campers must be in 'Approved' state.");
            }


            // check if camperId(s) belong to the registration
            var validCamperIds = registration.RegistrationCampers.Select(rc => rc.camperId).ToHashSet();

            // check optional choices
            if (request.OptionalChoices != null)
            {
                foreach (var choice in request.OptionalChoices)
                {
                    if (!validCamperIds.Contains(choice.CamperId))
                    {
                        throw new InvalidOperationException($"Camper ID {choice.CamperId} không thuộc đơn đăng ký này (Registration ID: {registrationId}).");
                    }
                }
            }

            // validate transport choices
            var transportScheduleIds = request.TransportChoices?.Select(t => t.TransportScheduleId).Distinct().ToList() ?? new List<int>();
            var locationIds = request.TransportChoices?.Select(t => t.LocationId).Distinct().ToList() ?? new List<int>();

            // get schedules and locations from db
            var schedules = await _unitOfWork.TransportSchedules.GetQueryable()
                .Where(t => transportScheduleIds.Contains(t.transportScheduleId))
                .Include(t => t.route).ThenInclude(r => r.RouteStops)
                .Include(t => t.vehicle)
                .Include(t => t.CamperTransports) 
                .ToListAsync();

            var locations = await _unitOfWork.Locations.GetQueryable()
                .Where(l => locationIds.Contains(l.locationId))
                .ToListAsync();

            if (request.TransportChoices != null && request.TransportChoices.Any())
            {
                foreach (var choice in request.TransportChoices)
                {
                    if (!validCamperIds.Contains(choice.CamperId))
                    {
                        throw new InvalidOperationException($"Camper ID {choice.CamperId} trong danh sách đưa đón không thuộc đơn đăng ký này.");
                    }

                    // validate Schedule
                    var schedule = schedules.FirstOrDefault(s => s.transportScheduleId == choice.TransportScheduleId);
                    if (schedule == null)
                        throw new KeyNotFoundException($"Không tìm thấy lịch trình vận chuyển ID {choice.TransportScheduleId}.");

                    if (schedule.campId != registration.campId)
                        throw new InvalidOperationException($"Lịch trình vận chuyển {choice.TransportScheduleId} không thuộc về trại của đơn đăng ký.");

                    // validate location (pickup point)
                    var location = locations.FirstOrDefault(l => l.locationId == choice.LocationId);
                    if (location == null)
                        throw new KeyNotFoundException($"Không tìm thấy điểm đón ID {choice.LocationId}.");

                    // check if location in transportSchedule route
                    var isValidStop = schedule.route?.RouteStops.Any(rs => rs.locationId == choice.LocationId) ?? false;
                    if (!isValidStop)
                        throw new InvalidOperationException($"Điểm đón {location.name} không nằm trong tuyến đường của lịch trình {choice.TransportScheduleId}.");

                    // check capacity
                    if (schedule.vehicle != null && schedule.vehicle.capacity <= schedule.CamperTransports.Count)
                    {
                        throw new InvalidOperationException($"Lịch trình {choice.TransportScheduleId} đã hết chỗ.");
                    }
                }
            }

            // check old transaction (reuse link if status = Pending)
            var existingPendingTransaction = await _unitOfWork.Transactions.GetQueryable()
                .Where(t => t.registrationId == registrationId && t.status == TransactionStatus.Pending.ToString())
                .OrderByDescending(t => t.transactionTime)
                .FirstOrDefaultAsync();

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

                    // list of activityScheduleId that user choose
                    // if null -> cancel all
                    var requestedScheduleIds = request.OptionalChoices?.Select(x => x.ActivityScheduleId).ToHashSet() ?? new HashSet<int>();

                    // get existed activity in the list
                    var existingActivities = registration.RegistrationOptionalActivities.ToList();

                    /*
                     * optional activity already in db but not in request
                     * cancel unused activity
                     */
                    foreach (var dbItem in existingActivities)
                    {
                        // if request null and activity status = holding in db
                        if (!requestedScheduleIds.Contains(dbItem.activityScheduleId) && dbItem.status == "Holding")
                        {
                            // return capacity
                            if (dbItem.activitySchedule != null && dbItem.activitySchedule.currentCapacity > 0)
                            {
                                dbItem.activitySchedule.currentCapacity -= 1;
                                await _unitOfWork.ActivitySchedules.UpdateAsync(dbItem.activitySchedule);
                            }

                            // cahange to cancel
                            dbItem.status = "Cancelled";
                            await _unitOfWork.RegistrationOptionalActivities.UpdateAsync(dbItem);
                        }
                    }

                    // add new - reactivate - keep
                    if (request.OptionalChoices != null)
                    {
                        // load all schedule info to check max capacity
                        var allSchedules = await _unitOfWork.ActivitySchedules.GetQueryable()
                            .Where(s => requestedScheduleIds.Contains(s.activityScheduleId))
                            .ToListAsync();

                        foreach (var choice in request.OptionalChoices)
                        {
                            var schedule = allSchedules.FirstOrDefault(s => s.activityScheduleId == choice.ActivityScheduleId)
                                ?? throw new KeyNotFoundException($"Activity Schedule {choice.ActivityScheduleId} not found.");

                            if (!schedule.isOptional)
                                throw new InvalidOperationException($"Schedule {choice.ActivityScheduleId} is not optional.");

                            // check if record is in db
                            var existingRecord = existingActivities
                                .FirstOrDefault(x => x.activityScheduleId == choice.ActivityScheduleId && x.camperId == choice.CamperId);

                            // if existed
                            if (existingRecord != null)
                            {
                                // if old status = cancel or reject -> reactivate
                                if (existingRecord.status == "Cancelled" || existingRecord.status == "Rejected")
                                {
                                    // slot capacity check
                                    if (schedule.currentCapacity >= schedule.maxCapacity)
                                        throw new InvalidOperationException($"Activity {schedule.activityScheduleId} is full.");

                                    schedule.currentCapacity = (schedule.currentCapacity ?? 0) + 1;
                                    await _unitOfWork.ActivitySchedules.UpdateAsync(schedule);

                                    existingRecord.status = "Holding";
                                    await _unitOfWork.RegistrationOptionalActivities.UpdateAsync(existingRecord);
                                }

                                // if status = holding -> do nothing
                                else if (existingRecord.status == "Holding"){ }
                            }

                            // if not existed
                            else
                            {
                                // slot capacity check
                                if (schedule.currentCapacity >= schedule.maxCapacity)
                                    throw new InvalidOperationException($"Activity {schedule.activityScheduleId} is full.");

                                schedule.currentCapacity = (schedule.currentCapacity ?? 0) + 1;
                                await _unitOfWork.ActivitySchedules.UpdateAsync(schedule);

                                // create new record
                                var newOptional = new RegistrationOptionalActivity
                                {
                                    registrationId = registration.registrationId,
                                    camperId = choice.CamperId,
                                    activityScheduleId = choice.ActivityScheduleId,
                                    status = "Holding",
                                    createdTime = DateTime.UtcNow
                                };
                                await _unitOfWork.RegistrationOptionalActivities.CreateAsync(newOptional);
                            }
                        }
                    }


                    // TRANSPORT CHOICES
                    // reset all requestTransport = false
                    foreach (var rc in registration.RegistrationCampers)
                    {
                        rc.requestTransport = false;
                        await _unitOfWork.RegistrationCampers.UpdateAsync(rc);
                    }

                    // remove old pending/assigned transports for this registration to avoid duplicates or stale data
                    var oldTransports = await _unitOfWork.CamperTransports.GetQueryable()
                        .Where(ct => registration.RegistrationCampers.Select(rc => rc.camperId).Contains(ct.camperId) &&
                                     ct.transportSchedule.campId == registration.campId &&
                                     ct.status == CamperTransportStatus.Assigned.ToString())
                        .ToListAsync();

                    if (oldTransports.Any())
                    {
                        _unitOfWork.CamperTransports.RemoveRange(oldTransports);
                    }

                    // new choices
                    if (request.TransportChoices != null && request.TransportChoices.Any())
                    {
                        foreach (var choice in request.TransportChoices)
                        {
                            // update requestTransport
                            var registrationCamper = registration.RegistrationCampers
                                .FirstOrDefault(rc => rc.camperId == choice.CamperId);

                            if (registrationCamper != null)
                            {
                                registrationCamper.requestTransport = true;
                                await _unitOfWork.RegistrationCampers.UpdateAsync(registrationCamper);

                                // create new camperTransport 
                                var newCamperTransport = new CamperTransport
                                {
                                    camperId = choice.CamperId,
                                    transportScheduleId = choice.TransportScheduleId,
                                    stopLocationId = choice.LocationId,
                                    status = CamperTransportStatus.Assigned.ToString(),
                                    isAbsent = false,
                                };

                                await _unitOfWork.CamperTransports.CreateAsync(newCamperTransport);
                            }
                        }
                    }



                    // calculate price
                    var finalAmountDecimal = CalculateFinalPrice(registration);
                    int finalAmount = (int)Math.Round(finalAmountDecimal + optionalActivitiesTotalCost);

                    var newTransaction = new Transaction
                    {
                        amount = finalAmount,
                        transactionTime = DateTime.UtcNow,
                        status = TransactionStatus.Pending.ToString(),
                        method = "PayOS",
                        type = "Payment",
                        registrationId = registration.registrationId
                    };
                    await _unitOfWork.Transactions.CreateAsync(newTransaction);
                    await _unitOfWork.CommitAsync(); // Commit for TransactionId

                    // Update Transaction Code
                    long uniqueOrderCode = long.Parse($"{newTransaction.transactionId}{DateTime.Now:fff}");
                    newTransaction.transactionCode = uniqueOrderCode.ToString();
                    await _unitOfWork.Transactions.UpdateAsync(newTransaction);

                    // Update Registration Status
                    registration.status = RegistrationStatus.PendingPayment.ToString();
                    await _unitOfWork.Registrations.UpdateAsync(registration);

                    await _unitOfWork.CommitAsync();

                    // Handle URL Redirect
                    string returnUrl;
                    string cancelUrl;

                    if (isMobile)
                    {
                        // use Mobile URLs
                        string baseApiUrl = _configuration["ApiBaseUrl"]
                            ?? throw new InvalidOperationException("ApiBaseUrl is not configured.");

                        returnUrl = _configuration["PayOS:MobileReturnUrl"]?.Replace("{API_BASE_URL}", baseApiUrl)
                            ?? $"{baseApiUrl}/api/payment/mobile-callback";

                        cancelUrl = _configuration["PayOS:MobileCancelUrl"]?.Replace("{API_BASE_URL}", baseApiUrl)
                            ?? $"{baseApiUrl}/api/payment/mobile-callback?status=CANCELLED";
                    }
                    else
                    {
                        returnUrl = _configuration["PayOS:ReturnUrl"] ?? "";
                        cancelUrl = _configuration["PayOS:CancelUrl"] ?? "";
                    }

                    // call payOS to create link
                    var paymentData = new PaymentData(
                        orderCode: uniqueOrderCode,
                        amount: finalAmount,
                        description: $"Thanh toan don #{registration.registrationId}",
                        items: new List<ItemData> {
                    new ItemData($"Trai he {registration.camp.name}", registration.RegistrationCampers.Count, (int)finalAmountDecimal)
                        },
                        cancelUrl: cancelUrl,
                        returnUrl: returnUrl
                    );

                    CreatePaymentResult createPaymentResult = await _payOSService.CreatePaymentLink(paymentData);

                    // Commit all transaction db
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
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }
        public async Task<IEnumerable<RegistrationResponseDto>> GetRegistrationByCampIdAsync(int campId)
        {
            var campExist = await _unitOfWork.Camps.GetByIdAsync(campId) != null;

            if (!campExist)
            {
                throw new KeyNotFoundException($"Camp with ID {campId} not found.");
            }

            var registrationEntities = await _unitOfWork.Registrations.GetQueryable()
                .Where(r => r.campId == campId)
                .Include(r => r.camp)
                .Include(r => r.RegistrationCampers).ThenInclude(rc => rc.camper)
                .Include(r => r.appliedPromotion)
                .Include(r => r.RegistrationOptionalActivities)
                .ToListAsync();

            var responseDtos = _mapper.Map<IEnumerable<RegistrationResponseDto>>(registrationEntities).ToList();

            // get final price for each registration
            for (int i = 0; i < registrationEntities.Count; i++)
            {
                responseDtos[i].FinalPrice = CalculateFinalPrice(registrationEntities[i]);
            }

            return responseDtos;
        }

        public async Task<IEnumerable<RegistrationResponseDto>> GetUserRegistrationHistoryAsync()
        {
            var currentUserId = _userContextService.GetCurrentUserId();

            if (!currentUserId.HasValue)
            {
                throw new UnauthorizedAccessException("Không thể lấy thông tin người dùng từ token. Xin hãy đăng nhập lại!");
            }

            var registrationEntities = await _unitOfWork.Registrations.GetQueryable()
                .Where(r => r.userId == currentUserId.Value)
                .OrderByDescending(r => r.registrationCreateAt)
                .Include(r => r.camp)
                .Include(r => r.RegistrationCampers).ThenInclude(rc => rc.camper)
                .Include(r => r.appliedPromotion)
                .Include(r => r.RegistrationOptionalActivities)
                .ToListAsync();

            var responseDtos = _mapper.Map<IEnumerable<RegistrationResponseDto>>(registrationEntities).ToList();

            // get final price
            for (int i = 0; i < registrationEntities.Count; i++)
            {
                responseDtos[i].FinalPrice = CalculateFinalPrice(registrationEntities[i]);
            }

            return responseDtos;
        }

        #region Private Methods

        private async Task<Registration> GetRegistrationForPaymentAsync(int registrationId)
        {
            var registration = await _unitOfWork.Registrations.GetQueryable()
                .Include(r => r.camp)
                .Include(r => r.RegistrationCampers).ThenInclude(rc => rc.camper)
                .Include(r => r.appliedPromotion)
                .Include(r => r.RegistrationOptionalActivities)
                    .ThenInclude(roa => roa.activitySchedule)
                .FirstOrDefaultAsync(r => r.registrationId == registrationId);

            if (registration == null) throw new KeyNotFoundException($"Registration with ID {registrationId} not found.");
            return registration;
        }

        private async Task ValidateCamperNotAlreadyRegisteredAsync(int campId, int camperId)
        {
            // check if camper available to register
            var isAlreadyRegistered = await _unitOfWork.Registrations.GetQueryable()
                .Where(r => r.campId == campId &&
                            r.RegistrationCampers.Any(rc => rc.camperId == camperId) &&
                            (r.status == RegistrationStatus.Approved.ToString() ||
                             r.status == RegistrationStatus.PendingApproval.ToString() ||
                             r.status == RegistrationStatus.PendingPayment.ToString()))
                .AnyAsync();

            if (isAlreadyRegistered)
            {
                // get camper name for more detail errors
                var camper = await _unitOfWork.Campers.GetByIdAsync(camperId);
                var camperName = camper?.camperName ?? $"ID {camperId}";
                throw new InvalidOperationException($"Camper {camperName} đã được đăng ký tham gia trại này hoặc đang có đơn chờ duyệt.");
            }
        }

        private decimal CalculateFinalPrice(Registration registration)
        {
            // Check necessary relationships
            if (registration.camp == null || registration.RegistrationCampers == null) return 0m; // FIX: Use RegistrationCampers

            // Base price (Camp Price * number of Campers)
            int baseAmount = (int)(registration.camp.price ?? 0) * registration.RegistrationCampers.Count;
            decimal finalAmount = baseAmount;
            decimal discount = 0m;

            // Handle promotion
            if (registration.appliedPromotionId.HasValue && registration.appliedPromotion != null)
            {
                var promotion = registration.appliedPromotion;

                // Check promotion status and time
                if (promotion.status == "Active" &&
                    (!promotion.startDate.HasValue || promotion.startDate.Value.ToDateTime(TimeOnly.MinValue) <= DateTime.UtcNow) &&
                    (!promotion.endDate.HasValue || promotion.endDate.Value.ToDateTime(TimeOnly.MinValue) >= DateTime.UtcNow))
                {
                    if (promotion.percent.HasValue)
                        discount = (decimal)baseAmount * (promotion.percent.Value / 100);

                    // Max discount limit
                    if (promotion.maxDiscountAmount.HasValue && discount > promotion.maxDiscountAmount.Value)
                        discount = promotion.maxDiscountAmount.Value;
                }

                finalAmount = finalAmount - discount;
            }

            return Math.Max(0m, finalAmount);
        }

        #endregion
    }
}