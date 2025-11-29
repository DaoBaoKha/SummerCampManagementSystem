using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Net.payOS;
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
        private readonly PayOS _payOS;
        private readonly IConfiguration _configuration;
        private readonly string _checksumKey;

        public PaymentService(IUnitOfWork unitOfWork, PayOS payOS, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _payOS = payOS;
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
                WebhookData verifiedData = _payOS.verifyPaymentWebhookData(webhookToVerify);

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

                    transaction.status = TransactionStatus.Confirmed.ToString();

                    /*
                    * STATUS REGISTRATION & REGISTRATION CAMPER = CONFIRMED
                    */
                    registration.status = RegistrationStatus.Confirmed.ToString();

                    foreach (var camperLink in registration.RegistrationCampers)
                    {
                        // only update camper at "Approved"
                        if (camperLink.status == RegistrationCamperStatus.Approved.ToString())
                        {
                            camperLink.status = RegistrationCamperStatus.Confirmed.ToString();
                            await _unitOfWork.RegistrationCampers.UpdateAsync(camperLink);
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
                        }
                    }

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
                    await _unitOfWork.Transactions.UpdateAsync(transaction);
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
                PaymentLinkInformation linkInfo = await _payOS.getPaymentLinkInformation(orderCode);

                // check status
                if (linkInfo.status == "PAID")
                {
                    return $"{BaseDeepLink}/success?{queryParams}"; 
                }
                else
                {
                    // (CANCELLED, EXPIRED, FAILED)
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
            string confirmationResult = await _payOS.confirmWebhook(url);

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
                PaymentLinkInformation linkInfo = await _payOS.getPaymentLinkInformation(orderCode);

                // check status of PayOS Server
                if (linkInfo.status == "PAID" && linkInfo.amountPaid > 0)
                {
                    response.IsSuccess = true;
                    response.Status = linkInfo.status;
                    response.Message = "Thanh toán thành công.";
                }
                else
                {
                    response.IsSuccess = false;
                    response.Status = linkInfo.status;
                    response.Message = "Giao dịch chưa hoàn tất.";
                    response.Detail = "Bạn có thể thử thanh toán lại từ lịch sử đăng ký.";

                    string orderCodeString = orderCode.ToString();
                    var transaction = await _unitOfWork.Transactions.GetQueryable()
                        .FirstOrDefaultAsync(t => t.transactionCode == orderCodeString);

                    if (transaction != null && transaction.status == TransactionStatus.Pending.ToString())
                    {
                        transaction.status = TransactionStatus.Failed.ToString();
                        await _unitOfWork.Transactions.UpdateAsync(transaction);
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
    }
}
