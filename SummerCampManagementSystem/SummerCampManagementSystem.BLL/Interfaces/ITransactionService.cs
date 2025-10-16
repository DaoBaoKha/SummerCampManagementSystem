using SummerCampManagementSystem.BLL.DTOs.PayOS;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface ITransactionService 
    {
        Task HandlePayOSWebhook(PayOSWebhookRequestDto webhookRequest);
        string ProcessPaymentMobileCallback(PayOSCallbackRequestDto callbackData);
        string ProcessPaymentMobileCallbackRaw(string rawQueryString);
        Task<string> ConfirmUrlAsync(string url);
        Task<WebCallbackResponseDto> ProcessPaymentWebsiteCallbackRaw(string rawQueryString);

    }
}
