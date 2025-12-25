using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Net.payOS.Types;
using SummerCampManagementSystem.BLL.DTOs.Refund;
using SummerCampManagementSystem.BLL.DTOs.Registration;
using SummerCampManagementSystem.BLL.Exceptions;
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
        private readonly IRefundService _refundService;

        public RegistrationService(IUnitOfWork unitOfWork, IValidationService validationService,
            IPayOSService payOSService, IConfiguration configuration, IMapper mapper, 
            IUserContextService userContextService, IRefundService refundService)
        {
            _unitOfWork = unitOfWork;
            _validationService = validationService;
            _payOSService = payOSService;
            _configuration = configuration;
            _mapper = mapper;
            _userContextService = userContextService;
            _refundService = refundService;
        }

        public async Task<RegistrationResponseDto> CreateRegistrationAsync(CreateRegistrationRequestDto request)
        {
            var camp = await _unitOfWork.Camps.GetByIdAsync(request.CampId)
                ?? throw new NotFoundException($"Camp with ID {request.CampId} not found.");

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
                    ?? throw new NotFoundException($"Camper with ID {camperId} not found.");

                // validate age
                await ValidateCamperAge(camper, camp);

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
            var registration = await _unitOfWork.Registrations.GetWithCampersAsync(registrationId)
                 ?? throw new NotFoundException($"Registration with ID {registrationId} not found.");

            if (registration.status != RegistrationStatus.PendingApproval.ToString())
            {
                throw new BusinessRuleException("Only 'PendingApproval' registrations can be approved.");
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

        public async Task<RegistrationResponseDto> RejectRegistrationAsync(RejectRegistrationRequestDto dto)
        {
            // get registration with campers
            var registration = await _unitOfWork.Registrations.GetDetailsForStatusUpdateAsync(dto.RegistrationId);

            if (registration == null)
                throw new NotFoundException($"Không tìm thấy đơn đăng ký ID {dto.RegistrationId}.");

            // validation
            if (registration.status != RegistrationStatus.PendingApproval.ToString())
            {
                throw new BadRequestException("Chỉ có thể từ chối đơn khi đang ở trạng thái 'PendingApproval'.");
            }

            bool isPartialReject = dto.CamperIds != null && dto.CamperIds.Any();

            // reject logic
            if (isPartialReject)
            {
                // reject specific campers only
                var validCamperIds = registration.RegistrationCampers.Select(rc => rc.camperId).ToList();
                var invalidIds = dto.CamperIds!.Except(validCamperIds).ToList();

                if (invalidIds.Any())
                {
                    throw new NotFoundException($"Các trại viên có ID [{string.Join(", ", invalidIds)}] không thuộc đơn đăng ký này.");
                }

                foreach (var camperId in dto.CamperIds!)
                {
                    var camperLink = registration.RegistrationCampers.First(rc => rc.camperId == camperId);

                    // only reject if status is PendingApproval
                    if (camperLink.status == RegistrationCamperStatus.PendingApproval.ToString())
                    {
                        camperLink.status = RegistrationCamperStatus.Rejected.ToString();
                        camperLink.rejectReason = dto.RejectReason;

                        await _unitOfWork.RegistrationCampers.UpdateAsync(camperLink);
                    }
                }

                registration.status = RegistrationStatus.Rejected.ToString();

                registration.rejectReason = $"Từ chối {dto.CamperIds.Count} trại viên. Lý do: {dto.RejectReason}. Vui lòng cập nhật hồ sơ.";
                }
                else
                {
                    // reject whole regis
                    registration.status = RegistrationStatus.Rejected.ToString();
                    registration.rejectReason = dto.RejectReason;

                    foreach (var camperLink in registration.RegistrationCampers)
                    {
                        if (camperLink.status == RegistrationCamperStatus.PendingApproval.ToString())
                        {
                            camperLink.status = RegistrationCamperStatus.Rejected.ToString();
                            camperLink.rejectReason = dto.RejectReason;
                            await _unitOfWork.RegistrationCampers.UpdateAsync(camperLink);
                        }
                    }
                }

            await _unitOfWork.Registrations.UpdateAsync(registration);
            await _unitOfWork.CommitAsync();

            return await GetRegistrationByIdAsync(dto.RegistrationId)
                   ?? throw new NotFoundException("Registration not found after rejection.");
        }

        public async Task<RegistrationResponseDto?> UpdateRegistrationAsync(int id, UpdateRegistrationRequestDto request)
        {
            var existingRegistration = await _unitOfWork.Registrations.GetForUpdateAsync(id);

            if (existingRegistration == null) throw new NotFoundException($"Không tìm thấy đơn ID {id}.");

            if (existingRegistration.status != RegistrationStatus.PendingApproval.ToString() && existingRegistration.status != RegistrationStatus.Approved.ToString() &&
                    existingRegistration.status != RegistrationStatus.Rejected.ToString())
            {
                throw new BusinessRuleException($"Không thể cập nhật đơn ở trạng thái '{existingRegistration.status}'.");
            }

            if (request.appliedPromotionId.HasValue)
            {
                var promotion = await _unitOfWork.Promotions.GetByIdAsync(request.appliedPromotionId.Value);
                if (promotion == null)
                {
                    throw new NotFoundException($"Mã khuyến mãi ID {request.appliedPromotionId} không tồn tại.");
                }
            }

            // access DbContext for complex M-to-M operations
            var dbContext = (CampEaseDatabaseContext)_unitOfWork.GetDbContext();

            // clear existing navigation collection to avoid EF Core tracking issues
            if (existingRegistration.RegistrationCampers != null)
            {
                existingRegistration.RegistrationCampers.Clear();
            }

            // only attach the existing entity to the context
            dbContext.Registrations.Attach(existingRegistration);

            // get list camper(s) from db
            var oldCamperLinks = await dbContext.RegistrationCampers
                .Where(rc => rc.registrationId == id)
                .ToListAsync();

            // delete camper not in the new list
            var linksToRemove = oldCamperLinks
                .Where(rc => !request.CamperIds.Contains(rc.camperId)).ToList();

            if (linksToRemove.Any())
            {
                dbContext.RegistrationCampers.RemoveRange(linksToRemove);
            }

            // get camp for validation
            var camp = await _unitOfWork.Camps.GetByIdAsync(request.CampId)
                ?? throw new NotFoundException($"Camp with ID {request.CampId} not found.");

            // add camper(s) in the new list
            var existingCamperIds = oldCamperLinks.Select(rc => rc.camperId).ToList();
            var camperIdsToAdd = request.CamperIds
                .Where(camperId => !existingCamperIds.Contains(camperId)).ToList();

            foreach (var camperId in camperIdsToAdd)
            {
                // validation
                await ValidateCamperNotAlreadyRegisteredAsync(request.CampId, camperId);

                // get camper for age validation
                var camper = await _unitOfWork.Campers.GetByIdAsync(camperId)
                    ?? throw new NotFoundException($"Camper with ID {camperId} not found.");

                // validate age
                await ValidateCamperAge(camper, camp);

                var newLink = new RegistrationCamper
                {
                    registrationId = id,
                    camperId = camperId,
                    status = RegistrationCamperStatus.PendingApproval.ToString(),
                    rejectReason = null
                };
                // add into dbcontext
                dbContext.RegistrationCampers.Add(newLink);
            }

            bool isResubmit = existingRegistration.status == RegistrationStatus.Rejected.ToString();
            bool requiresReApproval = existingRegistration.status == RegistrationStatus.Approved.ToString() || isResubmit;

            if (requiresReApproval)
            {
                // old camper(s) to keep - reset status to PendingApproval
                var linksToKeep = oldCamperLinks.Where(rc => request.CamperIds.Contains(rc.camperId)).ToList();
                foreach (var link in linksToKeep)
                {
                    link.status = RegistrationCamperStatus.PendingApproval.ToString();
                    link.rejectReason = null;
                    // mark as modified so EF Core tracks the change
                    dbContext.Entry(link).State = EntityState.Modified;
                }
            }

            existingRegistration.campId = request.CampId;
            existingRegistration.appliedPromotionId = request.appliedPromotionId;
            existingRegistration.note = request.Note;

            if (requiresReApproval)
            {
                existingRegistration.status = RegistrationStatus.PendingApproval.ToString();
                existingRegistration.rejectReason = null;
            }

            await _unitOfWork.Registrations.UpdateAsync(existingRegistration);

            await _unitOfWork.CommitAsync();

            return await GetRegistrationByIdAsync(id);
        }
        public async Task<RegistrationResponseDto?> GetRegistrationByIdAsync(int id)
        {
            var registrationEntity = await _unitOfWork.Registrations.GetFullDetailsAsync(id);

            if (registrationEntity == null) return null;

            // Map data to DTO (AutoMapper will use the updated profile to extract campers from RegistrationCampers)
            var responseDto = _mapper.Map<RegistrationResponseDto>(registrationEntity);

            // Add final price
            responseDto.FinalPrice = CalculateFinalPrice(registrationEntity);

            return responseDto;
        }


        public async Task<IEnumerable<RegistrationResponseDto>> GetAllRegistrationsAsync()
        {
            var registrationEntities = await _unitOfWork.Registrations.GetAllWithDetailsAsync();

            // convert to list to use index in loop
            var entityList = registrationEntities.ToList();
            var responseDtos = _mapper.Map<IEnumerable<RegistrationResponseDto>>(entityList).ToList();

            for (int i = 0; i < entityList.Count; i++)
            {
                responseDtos[i].FinalPrice = CalculateFinalPrice(entityList[i]);
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
                throw new NotFoundException($"Registration with ID {registrationId} not found.");
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
                throw new BusinessRuleException($"Cannot cancel registration with status '{registration.status}'.");
            }

            // soft delete 
            registration.status = RegistrationStatus.Canceled.ToString();

            await _unitOfWork.Registrations.UpdateAsync(registration);
            await _unitOfWork.CommitAsync();

            return true;
        }

        public async Task<IEnumerable<RegistrationResponseDto>> GetRegistrationByStatusAsync(RegistrationStatus? status = null)
        {
            IEnumerable<Registration> registrationEntities;

            if (status.HasValue)
            {
                registrationEntities = await _unitOfWork.Registrations.GetByStatusAsync(status.Value.ToString());
            }
            else
            {
                // fallback to GetAll if no status
                registrationEntities = await _unitOfWork.Registrations.GetAllWithDetailsAsync();
            }

            var entityList = registrationEntities.ToList();
            var responseDtos = _mapper.Map<IEnumerable<RegistrationResponseDto>>(entityList).ToList();

            for (int i = 0; i < entityList.Count; i++)
            {
                responseDtos[i].FinalPrice = CalculateFinalPrice(entityList[i]);
            }

            return responseDtos;
        }

        public async Task<GeneratePaymentLinkResponseDto> GeneratePaymentLinkAsync(int registrationId, GeneratePaymentLinkRequestDto request, bool isMobile)
        {
            // load registration
            var registration = await _unitOfWork.Registrations.GetForPaymentAsync(registrationId);

            if (registration == null) throw new NotFoundException($"Registration with ID {registrationId} not found.");

            // validate Status
            if (registration.status != RegistrationStatus.Approved.ToString() &&
                registration.status != RegistrationStatus.PendingPayment.ToString())
            {
                throw new BusinessRuleException("Payment link can only be generated for 'Approved' or 'PendingPayment' registrations.");
            }

            // validate Campers
            if (!registration.RegistrationCampers.Any() ||
                !registration.RegistrationCampers.All(rc => rc.status == RegistrationCamperStatus.Approved.ToString()))
            {
                throw new BusinessRuleException("All campers must be in 'Approved' state.");
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
                        throw new BusinessRuleException($"Camper ID {choice.CamperId} không thuộc đơn đăng ký này (Registration ID: {registrationId}).");
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
                        throw new BusinessRuleException($"Camper ID {choice.CamperId} trong danh sách đưa đón không thuộc đơn đăng ký này.");
                    }

                    // validate Schedule
                    var schedule = schedules.FirstOrDefault(s => s.transportScheduleId == choice.TransportScheduleId);
                    if (schedule == null)
                        throw new NotFoundException($"Không tìm thấy lịch trình vận chuyển ID {choice.TransportScheduleId}.");

                    if (schedule.campId != registration.campId)
                        throw new BusinessRuleException($"Lịch trình vận chuyển {choice.TransportScheduleId} không thuộc về trại của đơn đăng ký.");

                    // validate location (pickup point)
                    var location = locations.FirstOrDefault(l => l.locationId == choice.LocationId);
                    if (location == null)
                        throw new NotFoundException($"Không tìm thấy điểm đón ID {choice.LocationId}.");

                    // check if location in transportSchedule route
                    var isValidStop = schedule.route?.RouteStops.Any(rs => rs.locationId == choice.LocationId) ?? false;
                    if (!isValidStop)
                        throw new BusinessRuleException($"Điểm đón {location.name} không nằm trong tuyến đường của lịch trình {choice.TransportScheduleId}.");

                    // check capacity
                    if (schedule.vehicle != null && schedule.vehicle.capacity <= schedule.CamperTransports.Count)
                    {
                        throw new BusinessRuleException($"Lịch trình {choice.TransportScheduleId} đã hết chỗ.");
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
                            .Include(s => s.activity)
                            .ToListAsync();

                        foreach (var choice in request.OptionalChoices)
                        {
                            var schedule = allSchedules.FirstOrDefault(s => s.activityScheduleId == choice.ActivityScheduleId)
                                ?? throw new NotFoundException($"Activity Schedule {choice.ActivityScheduleId} not found.");

                            if (schedule.activity?.activityType != "Optional")
                                throw new BusinessRuleException($"Schedule {choice.ActivityScheduleId} is not optional.");

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
                                        throw new BusinessRuleException($"Activity {schedule.activityScheduleId} is full.");

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
                                    throw new BusinessRuleException($"Activity {schedule.activityScheduleId} is full.");

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
                    // route through backend API for both success and cancel
                    // update DB and redirect to frontend
                    string returnUrl;
                    string cancelUrl;
                    string baseApiUrl = _configuration["ApiBaseUrl"]
                        ?? throw new BusinessRuleException("ApiBaseUrl is not configured.");

                    if (isMobile)
                    {
                        // Mobile: API callbacks
                        returnUrl = _configuration["PayOS:MobileReturnUrl"]?.Replace("{API_BASE_URL}", baseApiUrl)
                            ?? $"{baseApiUrl}/api/payment/mobile-callback";

                        cancelUrl = _configuration["PayOS:MobileCancelUrl"]?.Replace("{API_BASE_URL}", baseApiUrl)
                            ?? $"{baseApiUrl}/api/payment/mobile-callback?status=CANCELLED";
                    }
                    else
                    {
                        // Website: BOTH ReturnUrl and CancelUrl point to backend API
                        // process payment status and redirect to frontend
                        // eeusing existing PayOS:ReturnUrl and PayOS:CancelUrl config keys
                        returnUrl = _configuration["PayOS:ReturnUrl"]?.Replace("{API_BASE_URL}", baseApiUrl)
                            ?? $"{baseApiUrl}/api/payment/website-callback";

                        cancelUrl = _configuration["PayOS:CancelUrl"]?.Replace("{API_BASE_URL}", baseApiUrl)
                            ?? $"{baseApiUrl}/api/payment/website-callback?status=CANCELLED";
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
                throw new NotFoundException($"Camp with ID {campId} not found.");
            }

            var registrationEntities = await _unitOfWork.Registrations.GetByCampIdAsync(campId);
            var entityList = registrationEntities.ToList();

            var responseDtos = _mapper.Map<IEnumerable<RegistrationResponseDto>>(registrationEntities).ToList();

            // get final price for each registration
            for (int i = 0; i < entityList.Count; i++)
            {
                responseDtos[i].FinalPrice = CalculateFinalPrice(entityList[i]);
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

            var registrationEntities = await _unitOfWork.Registrations.GetHistoryByUserIdAsync(currentUserId.Value);
            var entityList = registrationEntities.ToList();

            var responseDtos = _mapper.Map<IEnumerable<RegistrationResponseDto>>(registrationEntities).ToList();

            // get final price
            for (int i = 0; i < entityList.Count; i++)
            {
                responseDtos[i].FinalPrice = CalculateFinalPrice(entityList[i]);
            }

            return responseDtos;
        }

        #region Private Methods

        private async Task ValidateCamperNotAlreadyRegisteredAsync(int campId, int camperId)
        {
            // check if camper available to register
            var isAlreadyRegistered = await _unitOfWork.Registrations.IsCamperRegisteredAsync(campId, camperId);

            if (isAlreadyRegistered)
            {
                // get camper name for more detail errors
                var camper = await _unitOfWork.Campers.GetByIdAsync(camperId);
                var camperName = camper?.camperName ?? $"ID {camperId}";
                throw new BusinessRuleException($"Camper {camperName} đã được đăng ký tham gia trại này. Chỉ có thể đăng ký lại nếu đơn đăng ký trước đó đã bị hủy.");
            }
        }

        private async Task ValidateCamperAge(Camper camper, Camp camp)
        {
            // check if camper has dob
            if (!camper.dob.HasValue)
            {
                throw new BusinessRuleException($"Không thể đăng ký. Camper {camper.camperName} chưa có thông tin ngày sinh.");
            }

            // skip validation if camp doesn't have age restrictions
            if (!camp.minAge.HasValue && !camp.maxAge.HasValue)
            {
                return;
            }

            // calculate age at time of registration
            var today = DateOnly.FromDateTime(DateTime.Now);
            int age = today.Year - camper.dob.Value.Year;
            if (today < camper.dob.Value.AddYears(age)) age--;

            // validate min age
            if (camp.minAge.HasValue && age < camp.minAge.Value)
            {
                throw new BusinessRuleException($"Không thể đăng ký. Camper {camper.camperName} ({age} tuổi) chưa đủ tuổi tối thiểu ({camp.minAge} tuổi) để tham gia trại {camp.name}.");
            }

            // validate max age
            if (camp.maxAge.HasValue && age > camp.maxAge.Value)
            {
                throw new BusinessRuleException($"Không thể đăng ký. Camper {camper.camperName} ({age} tuổi) vượt quá độ tuổi tối đa ({camp.maxAge} tuổi) để tham gia trại {camp.name}.");
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

        public async Task<CancelRegistrationResponseDto> CancelRegistrationAsync(int registrationId, CancelRegistrationRequestDto request)
        {
            // get current user
            var currentUserId = _userContextService.GetCurrentUserId();
            if (!currentUserId.HasValue)
                throw new UnauthorizedException("Người dùng chưa xác thực.");

            // get registration with full details using existing repository method
            var registration = await _unitOfWork.Registrations.GetFullDetailsAsync(registrationId);
            if (registration == null)
                throw new NotFoundException($"Không tìm thấy đơn đăng ký với ID {registrationId}.");

            // validate ownership
            if (registration.userId != currentUserId.Value)
                throw new UnauthorizedException("Bạn không có quyền hủy đơn đăng ký này.");

            // check if camp is
            var camp = registration.camp;
            if (camp.status == CampStatus.RegistrationClosed.ToString())
            {
                var currentDate = DateTime.UtcNow;
                if (currentDate >= camp.registrationEndDate.Value)
                {
                    throw new BusinessRuleException("Không thể hủy đăng ký sau khi thời gian đăng ký đã đóng.");
                }
            }

            // check if already cancelled or being cancelled
            if (registration.status == RegistrationStatus.Canceled.ToString())
                throw new BadRequestException("Đơn đăng ký đã được hủy.");

            if (registration.status == RegistrationStatus.PendingRefund.ToString())
                throw new BadRequestException("Yêu cầu hủy đơn đăng ký đang được xử lý.");

            // validate status allows cancellation
            var allowedStatuses = new[]
            {
                RegistrationStatus.PendingApproval.ToString(),
                RegistrationStatus.Approved.ToString(),
                RegistrationStatus.PendingPayment.ToString(),
                RegistrationStatus.Confirmed.ToString(),
                RegistrationStatus.Rejected.ToString()
            };

            if (!allowedStatuses.Contains(registration.status))
                throw new BadRequestException($"Không thể hủy đơn đăng ký ở trạng thái '{registration.status}'.");

            // check if payment has been confirmed
            var hasConfirmedPayment = registration.Transactions
                .Any(t => t.type == "Payment" && t.status == TransactionStatus.Confirmed.ToString());

            // unpaid Registration - Free Cancel
            if (!hasConfirmedPayment)
            {
                using (var transaction = await _unitOfWork.BeginTransactionAsync())
                {
                    try
                    {
                        // update registration campers to Canceled
                        foreach (var regCamper in registration.RegistrationCampers)
                        {
                            regCamper.status = RegistrationCamperStatus.Canceled.ToString();
                            await _unitOfWork.RegistrationCampers.UpdateAsync(regCamper);
                        }

                        // update optional activities to Canceled
                        // registrationOptionalActivity has status field
                        var optionalActivities = registration.RegistrationOptionalActivities;
                        foreach (var activity in optionalActivities)
                        {
                            activity.status = "Canceled";
                            // mark as modified using DbContext
                            var dbContext = _unitOfWork.GetDbContext();
                            dbContext.Entry(activity).State = EntityState.Modified;
                        }

                        // update all CamperTransports to Canceled status
                        var camperIds = registration.RegistrationCampers.Select(rc => rc.camperId).ToList();
                        if (camperIds.Any())
                        {
                            var camperTransports = await _unitOfWork.CamperTransports
                                .GetQueryable()
                                .Where(ct => camperIds.Contains(ct.camperId) && 
                                             ct.transportSchedule.campId == registration.campId)
                                .ToListAsync();

                            foreach (var transport in camperTransports)
                            {
                                transport.status = CamperTransportStatus.Canceled.ToString();
                                await _unitOfWork.CamperTransports.UpdateAsync(transport);
                            }

                            // release campers from Groups
                            var camperGroups = await _unitOfWork.CamperGroups.GetQueryable()
                                .Include(cg => cg.group)
                                .Where(cg => camperIds.Contains(cg.camperId) && 
                                             cg.group.campId == registration.campId && 
                                             cg.status == CamperGroupStatus.Active.ToString())
                                .ToListAsync();

                            var groupsToUpdate = new HashSet<int>();
                            foreach (var camperGroup in camperGroups)
                            {
                                // set to inactive to release camper from group
                                camperGroup.status = CamperGroupStatus.Inactive.ToString();
                                await _unitOfWork.CamperGroups.UpdateAsync(camperGroup);
                                
                                // track group for size update
                                groupsToUpdate.Add(camperGroup.groupId);
                            }

                            // update group currentSize
                            foreach (var groupId in groupsToUpdate)
                            {
                                var group = await _unitOfWork.Groups.GetByIdAsync(groupId);
                                if (group != null && group.currentSize.HasValue && group.currentSize.Value > 0)
                                {
                                    group.currentSize = group.currentSize.Value - 1;
                                    await _unitOfWork.Groups.UpdateAsync(group);
                                }
                            }

                            // release campers from Accommodations
                            var camperAccommodations = await _unitOfWork.CamperAccommodations.GetQueryable()
                                .Include(ca => ca.accommodation)
                                .Where(ca => camperIds.Contains(ca.camperId) && 
                                             ca.accommodation.campId == registration.campId && 
                                             ca.status == CamperAccommodationStatus.Active.ToString())
                                .ToListAsync();

                            foreach (var camperAccommodation in camperAccommodations)
                            {
                                // set to inactive to release camper from accommodation
                                camperAccommodation.status = CamperAccommodationStatus.Inactive.ToString();
                                await _unitOfWork.CamperAccommodations.UpdateAsync(camperAccommodation);
                            }
                        }

                        // update registration status to canceled
                        registration.status = RegistrationStatus.Canceled.ToString();
                        registration.rejectReason = request.Reason ?? "Đã hủy bởi người dùng";
                        await _unitOfWork.Registrations.UpdateAsync(registration);

                        await _unitOfWork.CommitAsync();
                        await transaction.CommitAsync();

                        return new CancelRegistrationResponseDto
                        {
                            RegistrationId = registrationId,
                            Status = RegistrationStatus.Canceled.ToString(),
                            RefundAmount = null,
                            RefundPercentage = null,
                            Message = "Đã hủy đơn đăng ký thành công. Chưa có thanh toán nào được thực hiện."
                        };
                    }
                    catch (Exception)
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }

            // paid Registration - Requires Refund Process
            else
            {
                // validate bank info is provided
                if (!request.BankUserId.HasValue || request.BankUserId.Value <= 0)
                    throw new BusinessRuleException("Thông tin tài khoản ngân hàng là bắt buộc để xử lý hoàn tiền.");

                // validate bank user exists and belongs to current user
                var bankUser = await _unitOfWork.BankUsers.GetByIdAsync(request.BankUserId.Value);
                if (bankUser == null)
                    throw new NotFoundException($"Không tìm thấy tài khoản ngân hàng với ID {request.BankUserId.Value}.");

                if (bankUser.userId != currentUserId.Value)
                    throw new UnauthorizedException("Tài khoản ngân hàng đã chỉ định không thuộc về bạn.");

                using (var transaction = await _unitOfWork.BeginTransactionAsync())
                {
                    try
                    {
                        // update registration campers to Canceled immediately
                        foreach (var regCamper in registration.RegistrationCampers)
                        {
                            regCamper.status = RegistrationCamperStatus.Canceled.ToString();
                            await _unitOfWork.RegistrationCampers.UpdateAsync(regCamper);
                        }

                        // update optional activities to Canceled
                        var optionalActivities = registration.RegistrationOptionalActivities;
                        foreach (var activity in optionalActivities)
                        {
                            activity.status = "Canceled";
                            var dbContext = _unitOfWork.GetDbContext();
                            dbContext.Entry(activity).State = EntityState.Modified;
                        }

                        // release resources immediately when refund is requested
                        var camperIds = registration.RegistrationCampers.Select(rc => rc.camperId).ToList();
                        if (camperIds.Any())
                        {
                            // update all CamperTransports to Canceled status
                            var camperTransports = await _unitOfWork.CamperTransports
                                .GetQueryable()
                                .Where(ct => camperIds.Contains(ct.camperId) && 
                                             ct.transportSchedule.campId == registration.campId)
                                .ToListAsync();

                            foreach (var transport in camperTransports)
                            {
                                transport.status = CamperTransportStatus.Canceled.ToString();
                                await _unitOfWork.CamperTransports.UpdateAsync(transport);
                            }

                            // release campers from Groups
                            var camperGroups = await _unitOfWork.CamperGroups.GetQueryable()
                                .Include(cg => cg.group)
                                .Where(cg => camperIds.Contains(cg.camperId) && 
                                             cg.group.campId == registration.campId && 
                                             cg.status == CamperGroupStatus.Active.ToString())
                                .ToListAsync();

                            var groupsToUpdate = new HashSet<int>();
                            foreach (var camperGroup in camperGroups)
                            {
                                // set to inactive to release camper from group
                                camperGroup.status = CamperGroupStatus.Inactive.ToString();
                                await _unitOfWork.CamperGroups.UpdateAsync(camperGroup);
                                
                                // track group for size update
                                groupsToUpdate.Add(camperGroup.groupId);
                            }

                            // update group currentSize
                            foreach (var groupId in groupsToUpdate)
                            {
                                var group = await _unitOfWork.Groups.GetByIdAsync(groupId);
                                if (group != null && group.currentSize.HasValue && group.currentSize.Value > 0)
                                {
                                    group.currentSize = group.currentSize.Value - 1;
                                    await _unitOfWork.Groups.UpdateAsync(group);
                                }
                            }

                            // release campers from Accommodations
                            var camperAccommodations = await _unitOfWork.CamperAccommodations.GetQueryable()
                                .Include(ca => ca.accommodation)
                                .Where(ca => camperIds.Contains(ca.camperId) && 
                                             ca.accommodation.campId == registration.campId && 
                                             ca.status == CamperAccommodationStatus.Active.ToString())
                                .ToListAsync();

                            foreach (var camperAccommodation in camperAccommodations)
                            {
                                // set to inactive to release camper from accommodation
                                camperAccommodation.status = CamperAccommodationStatus.Inactive.ToString();
                                await _unitOfWork.CamperAccommodations.UpdateAsync(camperAccommodation);
                            }
                        }

                        // delegate to refund service for policy-based refund
                        var refundRequest = new CancelRequestDto
                        {
                            RegistrationId = registrationId,
                            BankUserId = request.BankUserId.Value,
                            Reason = request.Reason
                        };

                        var refundResult = await _refundService.RequestCancelAsync(refundRequest);

                        await _unitOfWork.CommitAsync();
                        await transaction.CommitAsync();

                        // calculate refund info for response
                        var refundCalc = await _refundService.CalculateRefundAsync(registrationId);

                        return new CancelRegistrationResponseDto
                        {
                            RegistrationId = registrationId,
                            Status = refundResult.Status,
                            RefundAmount = refundResult.RefundAmount,
                            RefundPercentage = refundCalc.RefundPercentage,
                            Message = $"Đã gửi yêu cầu hoàn tiền và nhả tài nguyên. {refundCalc.PolicyDescription}"
                        };
                    }
                    catch (Exception)
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
        }

        #endregion
    }
}