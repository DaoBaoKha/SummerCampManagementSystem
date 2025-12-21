using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Net.payOS.Types;
using SummerCampManagementSystem.BLL.DTOs.PayOS;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.Core.Enums;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;
using System.Web;

namespace SummerCampManagementSystem.BLL.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPayOSService _payOSService;
        private readonly IConfiguration _configuration;
        private readonly string _checksumKey;

        public PaymentService(IUnitOfWork unitOfWork, IPayOSService payOSService, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _payOSService = payOSService;
            _configuration = configuration;
            // checksumkey from config
            _checksumKey = _configuration["PayOS:ChecksumKey"] ??
                           throw new InvalidOperationException("PayOS:ChecksumKey is not configured.");
        }

        public async Task HandlePayOSWebhook(PayOSWebhookRequestDto webhookRequest)
        {
            try
            {
                var data = webhookRequest.data;
                // create a WebhookData object from the incoming request data
                var webhookDataForSdk = new WebhookData(
                    data.orderCode,
                    data.amount,
                    data.description,
                    data.accountNumber,
                    data.reference,
                    data.transactionDateTime,
                    data.currency,
                    data.paymentLinkId,
                    data.code,
                    data.desc,
                    data.counterAccountBankId,
                    data.counterAccountBankName,
                    data.counterAccountName,
                    data.counterAccountNumber,
                    data.virtualAccountName,
                    data.virtualAccountNumber
                );

                var webhookToVerify = new WebhookType(
                    code: webhookRequest.code,
                    desc: webhookRequest.desc,
                    data: webhookDataForSdk,
                    success: webhookRequest.success,
                    signature: webhookRequest.signature
                );

                // verify the webhook using PayOS SDK
                WebhookData verifiedData = _payOSService.VerifyPaymentWebhookData(webhookToVerify);

                // find transaction by using transactionCode column
                string orderCodeString = verifiedData.orderCode.ToString();
                var transaction = await _unitOfWork.Transactions.GetQueryable()
                    .FirstOrDefaultAsync(t => t.transactionCode == orderCodeString);

                // If transaction not found, or not pending, ignore.
                if (transaction == null || transaction.status != TransactionStatus.Pending.ToString() || !transaction.registrationId.HasValue)
                {
                    Console.WriteLine($"Webhook ignored: Transaction {orderCodeString} not found, not pending, or has no registration ID.");
                    return;
                }

                // Load Registration & RegistrationCampers
                var registration = await _unitOfWork.Registrations.GetQueryable()
                    .Include(r => r.RegistrationCampers)
                    .ThenInclude(rc => rc.camper)
                    .Include(r => r.camp) // get campId for assign group logic
                    .FirstOrDefaultAsync(r => r.registrationId == transaction.registrationId.Value);

                // respond to the webhook based on the verification result
                // successful payment
                if (verifiedData.code == "00")
                {
                    // check registration status
                    if (registration == null || registration.status != RegistrationStatus.PendingPayment.ToString())
                    {
                        Console.WriteLine($"Webhook ignored: Registration {transaction.registrationId.Value} not found or not in PendingPayment state.");
                        return;
                    }

                    /*
                    * STATUS REGISTRATION & REGISTRATION CAMPER = CONFIRMED
                    */

                    transaction.status = TransactionStatus.Confirmed.ToString();
                    registration.status = RegistrationStatus.Confirmed.ToString();

                    // get list of camperId(s) to assign group
                    var confirmedCamperIds = new List<int>();

                    foreach (var camperLink in registration.RegistrationCampers)
                    {
                        // only update camper at "Approved"
                       if (camperLink.status == RegistrationCamperStatus.Approved.ToString())
                       {
                            camperLink.status = RegistrationCamperStatus.Confirmed.ToString();
                            await _unitOfWork.RegistrationCampers.UpdateAsync(camperLink);
                            
                            // get camper status = confirmed
                            confirmedCamperIds.Add(camperLink.camperId);
                       }
                    }

                    // find optionalActivities with status holding
                    var optionalActivities = await _unitOfWork.RegistrationOptionalActivities.GetQueryable()
                        .Where(roa => roa.registrationId == registration.registrationId && roa.status == "Holding")
                        .ToListAsync();

                    if (optionalActivities.Any())
                    {
                        foreach (var activity in optionalActivities)
                        {
                            // change status from holding to confirmed
                            activity.status = "Confirmed";
                            await _unitOfWork.RegistrationOptionalActivities.UpdateAsync(activity);

                            // check duplicate in CamperActivity
                            var existingCamperActivity = await _unitOfWork.CamperActivities.GetQueryable()
                                .FirstOrDefaultAsync(ca => ca.camperId == activity.camperId &&
                                                           ca.activityScheduleId == activity.activityScheduleId);

                            if (existingCamperActivity == null)
                            {
                                var newCamperActivity = new CamperActivity
                                {
                                    camperId = activity.camperId,
                                    activityScheduleId = activity.activityScheduleId,
                                    participationStatus = "Approved"
                                };
                                await _unitOfWork.CamperActivities.CreateAsync(newCamperActivity);
                            }
                        }
                    }

                    if (registration.campId.HasValue && confirmedCamperIds.Any())
                    {
                        int campId = registration.campId.Value;

                        // load groups
                        var groups = await _unitOfWork.Groups.GetQueryable()
                            .Where(g => g.campId == campId)
                            .Include(g => g.CamperGroups)
                            .ToListAsync();

                        // auto assign group
                        var unassignedGroupCamperIds = await AssignCampersToGroupsAsync(campId, confirmedCamperIds, registration.RegistrationCampers, groups);


                        // get accommodations
                        var accommodations = await _unitOfWork.Accommodations.GetQueryable()
                            .Where(a => a.campId == campId && a.isActive == true)
                            .Include(a => a.CamperAccommodations)
                            .ToListAsync();

                        // auto assign accommodation
                        var unassignedAccCamperIds = await AssignCampersToAccommodationsAsync(campId, confirmedCamperIds, accommodations);


                        // update status camper if no group or no accommodation assigned
                        if (unassignedGroupCamperIds.Any())
                        {
                            foreach (var camperLink in registration.RegistrationCampers)
                            {
                                if (unassignedGroupCamperIds.Contains(camperLink.camperId))
                                {
                                    camperLink.status = RegistrationCamperStatus.PendingAssignGroup.ToString();
                                    await _unitOfWork.RegistrationCampers.UpdateAsync(camperLink);
                                }
                            }
                        }
                    }


                    // if no unassignedCamperIds -> all status = Confirmed

                    await _unitOfWork.Transactions.UpdateAsync(transaction);
                    await _unitOfWork.Registrations.UpdateAsync(registration);
                    await _unitOfWork.CommitAsync();
                }

                // payment failed or cancelled
                else
                {
                    Console.WriteLine($"Payment failed or cancelled for order {orderCodeString}. Code: {verifiedData.code}");

                    // still keep optional activity slot at holding so user can make payment again

                    transaction.status = TransactionStatus.Failed.ToString();
                    registration.status = RegistrationStatus.Approved.ToString();
                    await _unitOfWork.Transactions.UpdateAsync(transaction);
                    await _unitOfWork.Registrations.UpdateAsync(registration);
                    await _unitOfWork.CommitAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Webhook processing failed: {ex.Message}");
                throw; 
            }
        }


        public async Task<string> ProcessPaymentMobileCallbackRaw(string rawQueryString)
        {
            var parsedQuery = HttpUtility.ParseQueryString(rawQueryString);
            if (parsedQuery["orderCode"] == null)
            {
                throw new ArgumentException("Required callback parameter (orderCode) is missing.");
            }

            long orderCode = long.TryParse(parsedQuery["orderCode"], out long oc) ? oc : 0;
            if (orderCode == 0)
            {
                throw new ArgumentException("Invalid orderCode.");
            }

            string orderCodeString = orderCode.ToString();
            var transaction = await _unitOfWork.Transactions.GetQueryable()
                .FirstOrDefaultAsync(t => t.transactionCode == orderCodeString);

            int? registrationId = transaction?.registrationId;

            return await ProcessPaymentMobileCallbackLogic(orderCode, registrationId); 
        }


        private async Task<string> ProcessPaymentMobileCallbackLogic(long orderCode, int? registrationId)
        {
            const string BaseDeepLink = "summercamp://payment";

            string queryParams = $"orderCode={orderCode}";
            if (registrationId.HasValue)
            {
                queryParams += $"&registrationId={registrationId.Value}";
            }

            try
            {
                // call getPaymentLinkInformation from PayOS
                PaymentLinkInformation linkInfo = await _payOSService.GetPaymentLinkInformation(orderCode);

                // check status
                if (linkInfo.status == "PAID")
                {
                    return $"{BaseDeepLink}/success?{queryParams}"; 
                }
                else
                {
                    // (CANCELLED, EXPIRED, FAILED)
                    // update database before redirect
                    string orderCodeString = orderCode.ToString();
                    var transaction = await _unitOfWork.Transactions.GetQueryable()
                        .FirstOrDefaultAsync(t => t.transactionCode == orderCodeString);

                    if (transaction != null && transaction.status == TransactionStatus.Pending.ToString())
                    {
                        /*
                         * STATUS TRANSACTION = FAILED
                         * STATUS REGISTRATION = APPROVED 
                         */
                        transaction.status = TransactionStatus.Failed.ToString();
                        await _unitOfWork.Transactions.UpdateAsync(transaction);

                        // update registration to approved if has registrationId
                        if (transaction.registrationId.HasValue)
                        {
                            var registration = await _unitOfWork.Registrations.GetByIdAsync(transaction.registrationId.Value);
                            if (registration != null && registration.status == RegistrationStatus.PendingPayment.ToString())
                            {
                                registration.status = RegistrationStatus.Approved.ToString();
                                await _unitOfWork.Registrations.UpdateAsync(registration);
                            }
                        }

                        await _unitOfWork.CommitAsync();
                    }

                    return $"{BaseDeepLink}/failure?{queryParams}&status={linkInfo.status}"; 
                }
            }
            catch (Exception ex)
            {
                string errorReason = Uri.EscapeDataString(ex.Message);
                return $"{BaseDeepLink}/failure?{queryParams}&reason=ApiError&details={errorReason}"; 
            }
        }

        /// <summary>
        /// send confirm url callback to payos server
        /// </summary>
        /// <param name="url">public url of callback api (like: https://yourdomain.com/api/payment/mobile-callback).</param>
        /// <returns>payos return confirmation</returns>
        public async Task<string> ConfirmUrlAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url) || !url.StartsWith("https"))
            {
                throw new ArgumentException("URL must be a valid HTTPS address.");
            }

            // this method to confirm URL Notification/Callback
            // return a string
            string confirmationResult = await _payOSService.ConfirmWebhook(url);

            return confirmationResult;
        }

        public async Task<WebCallbackResponseDto> ProcessPaymentWebsiteCallbackRaw(string rawQueryString)
        {
            var parsedQuery = HttpUtility.ParseQueryString(rawQueryString);

            // just use ordercode for api
            if (parsedQuery["orderCode"] == null)
            {
                throw new ArgumentException("Required callback parameter (orderCode) is missing from the URL.");
            }

            long orderCode = long.TryParse(parsedQuery["orderCode"], out long oc) ? oc : 0;
            var response = new WebCallbackResponseDto { OrderCode = orderCode };

            try
            {
                // use getpaymentLinkInformation
                PaymentLinkInformation linkInfo = await _payOSService.GetPaymentLinkInformation(orderCode);

                // check status of PayOS Server
                if (linkInfo.status == "PAID" && linkInfo.amountPaid > 0)
                {
                    response.IsSuccess = true;
                    response.Status = linkInfo.status;
                    response.Message = "Thanh toán thành công.";
                }
                else
                {
                    // payment failed, cancelled, or expired
                    response.IsSuccess = false;
                    response.Status = linkInfo.status;
                    response.Message = "Giao dịch chưa hoàn tất.";
                    response.Detail = "Bạn có thể thử thanh toán lại từ lịch sử đăng ký.";

                    string orderCodeString = orderCode.ToString();
                    var transaction = await _unitOfWork.Transactions.GetQueryable()
                        .FirstOrDefaultAsync(t => t.transactionCode == orderCodeString);

                    if (transaction != null && transaction.status == TransactionStatus.Pending.ToString())
                    {
                        /*
                         * STATUS TRANSACTION = FAILED
                         * STATUS REGISTRATION = APPROVED 
                         */
                        transaction.status = TransactionStatus.Failed.ToString();
                        await _unitOfWork.Transactions.UpdateAsync(transaction);

                        // update registration to approved if has registrationId
                        if (transaction.registrationId.HasValue)
                        {
                            var registration = await _unitOfWork.Registrations.GetByIdAsync(transaction.registrationId.Value);
                            if (registration != null && registration.status == RegistrationStatus.PendingPayment.ToString())
                            {
                                registration.status = RegistrationStatus.Approved.ToString();
                                await _unitOfWork.Registrations.UpdateAsync(registration);
                            }
                        }

                        await _unitOfWork.CommitAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Status = "ERROR";
                response.Message = "Lỗi hệ thống khi truy vấn trạng thái giao dịch.";
                response.Detail = ex.Message;
            }

            return response;
        }

        #region Private Methods

        /// <param name="campId">The ID of the camp.</param>
        /// <param name="camperIds">List of camper IDs to assign.</param>
        /// <param name="registrationCampers">List of RegistrationCampers for update.</param>
        /// <param name="availableGroups">List of available Group with CamperGroups included.</param>
        /// <returns>List of camper IDs with no group</returns>
        private async Task<List<int>> AssignCampersToGroupsAsync(int campId, List<int> camperIds, IEnumerable<RegistrationCamper> registrationCampers, List<Group> availableGroups)
        {

            if (!availableGroups.Any()) return camperIds; // no group -> no assign

            // get camper dateOfBirth
            var campers = await _unitOfWork.Campers.GetQueryable()
                .Where(c => camperIds.Contains(c.camperId))
                .ToListAsync();

            var newCamperGroups = new List<CamperGroup>();
            var groupsToUpdate = new List<Group>(); // get list groups to update currentSize
            var unassignedCamperIds = new List<int>();
            var today = DateOnly.FromDateTime(DateTime.Now);

            foreach (var camper in campers)
            {
                // if (registrationCampers.FirstOrDefault(rc => rc.camperId == camper.camperId)?.status != RegistrationCamperStatus.Confirmed.ToString()) continue;

                if (camper.dob == null)
                {
                    unassignedCamperIds.Add(camper.camperId);
                    continue; // no dob -> no assign
                }

                // calculate age
                int age = today.Year - camper.dob.Value.Year;
                if (today < camper.dob.Value.AddYears(age))
                {
                    age--;
                }

                // find suitable group based on Age and Capacity (maxSize)
                var suitableGroup = availableGroups
                    .FirstOrDefault(g =>
                        age >= g.minAge &&
                        age <= g.maxAge &&
                        (g.maxSize == 0 || g.CamperGroups.Count < g.maxSize)
                    );

                if (suitableGroup != null)
                {
                    var mapping = new CamperGroup
                    {
                        camperId = camper.camperId,
                        groupId = suitableGroup.groupId,
                        status = CamperGroupStatus.Active.ToString()
                    };

                    newCamperGroups.Add(mapping);

                    /*
                      logic "First Come First Served"
                      if group a maxSize = 10 and current has 9
                      camper a make payment successfully -> add into db -> add to group a memory list (list ảo)
                      camper b make payment successfully -> group a already 10/10 -> skip
                   */
                    suitableGroup.CamperGroups.Add(mapping);

                    // update currentSize
                    suitableGroup.currentSize = (suitableGroup.currentSize ?? 0) + 1;

                    // Đánh dấu nhóm này cần được cập nhật
                    if (!groupsToUpdate.Contains(suitableGroup))
                    {
                        groupsToUpdate.Add(suitableGroup);
                    }
                }
                else
                {
                    unassignedCamperIds.Add(camper.camperId); // no suitable group
                }
            }

            if (newCamperGroups.Any())
            {
                await _unitOfWork.CamperGroups.AddRangeAsync(newCamperGroups);
            }

            // update group currentSize
            foreach (var group in groupsToUpdate)
            {
                await _unitOfWork.Groups.UpdateAsync(group);
            }

            return unassignedCamperIds; // return list camperIds with no group
        }

        private async Task<List<int>> AssignCampersToAccommodationsAsync(int campId, List<int> camperIds, List<Accommodation> availableAccommodations)
        {
            if (!availableAccommodations.Any()) return camperIds;

            var newCamperAccommodations = new List<CamperAccommodation>();
            var unassignedCamperIds = new List<int>();

            foreach (var camperId in camperIds)
            {
                // logic "First Come First Served"
                var suitableAccommodation = availableAccommodations
                    .FirstOrDefault(a => a.capacity > a.CamperAccommodations.Count);

                if (suitableAccommodation != null)
                {
                    var mapping = new CamperAccommodation
                    {
                        camperId = camperId,
                        accommodationId = suitableAccommodation.accommodationId,
                        status = "Active", 
                    };

                    newCamperAccommodations.Add(mapping);

                    suitableAccommodation.CamperAccommodations.Add(mapping);
                }
                else
                {
                    unassignedCamperIds.Add(camperId); // no suitable accommodation
                }
            }

            if (newCamperAccommodations.Any())
            {
                await _unitOfWork.CamperAccommodations.AddRangeAsync(newCamperAccommodations);
            }

            return unassignedCamperIds;
        }        

        #endregion
    }
}
