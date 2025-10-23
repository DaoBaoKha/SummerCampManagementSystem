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
    public class TransactionService : ITransactionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly PayOS _payOS;
        private readonly IConfiguration _configuration;
        private readonly string _checksumKey;

        public TransactionService(IUnitOfWork unitOfWork, PayOS payOS, IConfiguration configuration)
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

                // respond to the webhook based on the verification result
                if (verifiedData.code == "00")
                {
                    int registrationId;
                    long uniqueOrderCode = verifiedData.orderCode;

                    // find transaction by using transactionCode column
                    string orderCodeString = uniqueOrderCode.ToString();
                    var transaction = await _unitOfWork.Transactions.GetQueryable()
                        .FirstOrDefaultAsync(t => t.transactionCode == orderCodeString);

                    if (transaction == null || transaction.status != "Pending")
                    {
                        return; 
                    }

                    if (!transaction.registrationId.HasValue) return;

                    registrationId = transaction.registrationId.Value;

                    var registration = await _unitOfWork.Registrations.GetByIdAsync(transaction.registrationId.Value);

                    if (registration == null || registration.status != RegistrationStatus.PendingPayment.ToString())
                    {
                        return;
                    }

                    transaction.status = "Completed";
                    registration.status = RegistrationStatus.Completed.ToString();


                    // find optionalActivities with status holding
                    var optionalActivities = await _unitOfWork.RegistrationOptionalActivities.GetQueryable()
                        .Where(roa => roa.registrationId == registrationId && roa.status == "Holding")
                        .ToListAsync();

                    if (optionalActivities.Any())
                    {
                        foreach (var activity in optionalActivities)
                        {
                            // change status from holding to confirmed
                            activity.status = "Confirmed";
                            _unitOfWork.RegistrationOptionalActivities.UpdateAsync(activity);
                        }
                    }

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

        public string ProcessPaymentMobileCallbackRaw(string rawQueryString)
        {
            // get raw query into string
            var parsedQuery = HttpUtility.ParseQueryString(rawQueryString);

            // check signature
            if (parsedQuery["signature"] == null || parsedQuery["orderCode"] == null || parsedQuery["status"] == null)
            {
                throw new ArgumentException("Required callback parameters (signature, orderCode, status) are missing from the URL.");
            }

            var callbackData = new PayOSCallbackRequestDto
            {
                Code = parsedQuery["code"] ?? "",
                Id = parsedQuery["id"] ?? "",
                // Parse boolean/int an toàn
                Cancel = bool.TryParse(parsedQuery["cancel"], out bool cancel) && cancel,
                Status = parsedQuery["status"] ?? "",
                OrderCode = int.TryParse(parsedQuery["orderCode"], out int orderCode) ? orderCode : 0,
                Signature = parsedQuery["signature"] ?? ""
            };

            return ProcessPaymentMobileCallback(callbackData);
        }

        public string ProcessPaymentMobileCallback(PayOSCallbackRequestDto callbackData)
        {
            const string BaseDeepLink = "yourapp://payment";

            // use webhooktype to use verifyPaymentWebhookData
            var callbackAsWebhook = new WebhookType(
                code: callbackData.Code,
                desc: "",
                success: callbackData.Status == "PAID",
                signature: callbackData.Signature,
            
            // enough constructor for webhookdata
                data: new WebhookData(
                orderCode: callbackData.OrderCode,
                amount: 0, // provide a default or actual value as needed
                description: "",
                accountNumber: "",
                reference: "",
                transactionDateTime: "",
                currency: "",
                paymentLinkId: "",
                code: callbackData.Code,
                desc: "",
                counterAccountBankId: null,
                counterAccountBankName: null,
                counterAccountName: null,
                counterAccountNumber: null,
                virtualAccountName: null,
                virtualAccountNumber: ""
                )
            );

            try
            {
                // use verifyPaymentWebhookData for signature verification
                // this method will verify signature and throw exception
                WebhookData verifiedData = _payOS.verifyPaymentWebhookData(callbackAsWebhook);

                string deepLinkPath;
                string queryParams = $"orderCode={callbackData.OrderCode}";

                if (verifiedData.code == "00") 
                {
                    deepLinkPath = "success";
                }
                else
                {
                    deepLinkPath = "failure";
                    queryParams += $"&status={callbackData.Status}&payosCode={verifiedData.code}";
                }

                return $"{BaseDeepLink}/{deepLinkPath}?{queryParams}";
            }
            catch (Exception ex)
            {
                // signature mismatch error or others
                string errorReason = Uri.EscapeDataString(ex.Message);

                if (ex.Message.Contains("Signature Mismatch"))
                {
                    return $"{BaseDeepLink}/failure?orderCode={callbackData.OrderCode}&reason=InvalidSignature";
                }

                return $"{BaseDeepLink}/failure?reason=SystemError&details={errorReason}";
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

            int orderCode = int.TryParse(parsedQuery["orderCode"], out int oc) ? oc : 0;
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
                    response.Message = "Giao dịch đang chờ xử lý hoặc thất bại.";
                    response.Detail = $"Trạng thái PayOS: {linkInfo.status}";
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
