using Net.payOS;
using Net.payOS.Types;
using SummerCampManagementSystem.BLL.Interfaces;

namespace SummerCampManagementSystem.BLL.Services
{
    public class PayOSService : IPayOSService
    {
        private readonly PayOS _payOS;

        public PayOSService(PayOS payOS)
        {
            _payOS = payOS;
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
            return await _payOS.createPaymentLink(paymentData);
        }
    }
}