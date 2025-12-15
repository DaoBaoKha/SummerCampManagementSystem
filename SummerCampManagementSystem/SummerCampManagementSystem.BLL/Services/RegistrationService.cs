using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Net.payOS.Types;
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

            // add camper(s) in the new list
            var existingCamperIds = oldCamperLinks.Select(rc => rc.camperId).ToList();
            var camperIdsToAdd = request.CamperIds
                .Where(camperId => !existingCamperIds.Contains(camperId)).ToList();

            foreach (var camperId in camperIdsToAdd)
            {
                // validation
                await ValidateCamperNotAlreadyRegisteredAsync(request.CampId, camperId);

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

            var camp = await _unitOfWork.Camps.GetByIdAsync(request.CampId)
                ?? throw new NotFoundException($"Camp with ID {request.CampId} not found.");

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
                            .ToListAsync();

                        foreach (var choice in request.OptionalChoices)
                        {
                            var schedule = allSchedules.FirstOrDefault(s => s.activityScheduleId == choice.ActivityScheduleId)
                                ?? throw new NotFoundException($"Activity Schedule {choice.ActivityScheduleId} not found.");

                            if (!schedule.isOptional)
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
                    string returnUrl;
                    string cancelUrl;

                    if (isMobile)
                    {
                        // use Mobile URLs
                        string baseApiUrl = _configuration["ApiBaseUrl"]
                            ?? throw new BusinessRuleException("ApiBaseUrl is not configured.");

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
                throw new BusinessRuleException($"Camper {camperName} đã được đăng ký tham gia trại này hoặc đang có đơn chờ duyệt.");
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