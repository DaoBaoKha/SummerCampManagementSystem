using Net.payOS;
using Net.payOS.Types;
using SummerCampManagementSystem.BLL.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace SummerCampManagementSystem.BLL.Services
{
    public class PayOSService : IPayOSService
    {
        private readonly PayOS _payOS;
        private readonly ILogger<PayOSService> _logger;

        public PayOSService(PayOS payOS, ILogger<PayOSService> logger)
        {
            _payOS = payOS;
            _logger = logger;
        }

        public WebhookData VerifyPaymentWebhookData(WebhookType webhookType)
        {
            return _payOS.verifyPaymentWebhookData(webhookType);
        }

        public async Task<PaymentLinkInformation> GetPaymentLinkInformation(long orderCode)
        {
            return await _payOS.getPaymentLinkInformation(orderCode);
        }

        public async Task<string> ConfirmWebhook(string webhookUrl)
        {
            return await _payOS.confirmWebhook(webhookUrl);
        }

        public async Task<CreatePaymentResult> CreatePaymentLink(PaymentData paymentData)
        {
            try
            {
                _logger.LogInformation("=== PayOS CreatePaymentLink START ===");
                _logger.LogInformation("OrderCode: {OrderCode}", paymentData.orderCode);
                _logger.LogInformation("Amount: {Amount}", paymentData.amount);
                _logger.LogInformation("Description: {Description}", paymentData.description);
                _logger.LogInformation("Items: {Items}", JsonSerializer.Serialize(paymentData.items));
                _logger.LogInformation("ReturnUrl: {ReturnUrl}", paymentData.returnUrl);
                _logger.LogInformation("CancelUrl: {CancelUrl}", paymentData.cancelUrl);
                
                var result = await _payOS.createPaymentLink(paymentData);
                
                _logger.LogInformation("PayOS Response - CheckoutUrl: {CheckoutUrl}", result.checkoutUrl);
                _logger.LogInformation("=== PayOS CreatePaymentLink SUCCESS ===");
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError("=== PayOS CreatePaymentLink FAILED ===");
                _logger.LogError("Exception Type: {ExceptionType}", ex.GetType().FullName);
                _logger.LogError("Exception Message: {Message}", ex.Message);
                _logger.LogError("Exception StackTrace: {StackTrace}", ex.StackTrace);
                
                if (ex.InnerException != null)
                {
                    _logger.LogError("InnerException Type: {InnerType}", ex.InnerException.GetType().FullName);
                    _logger.LogError("InnerException Message: {InnerMessage}", ex.InnerException.Message);
                }
                
                throw;
            }
        }
    }
}