using Net.payOS.Types;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface IPayOSService
    {
        WebhookData VerifyPaymentWebhookData(WebhookType webhookType);
        Task<PaymentLinkInformation> GetPaymentLinkInformation(long orderCode);
        Task<string> ConfirmWebhook(string webhookUrl);
        Task<CreatePaymentResult> CreatePaymentLink(PaymentData paymentData);
    }
}