using SummerCampManagementSystem.BLL.DTOs.PayOS;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface IPaymentService 
    {
        Task HandlePayOSWebhook(PayOSWebhookRequestDto webhookRequest);
        string ProcessPaymentMobileCallback(PayOSCallbackRequestDto callbackData);
        string ProcessPaymentMobileCallbackRaw(string rawQueryString);
        Task<string> ConfirmUrlAsync(string url);
    }
}
