using SummerCampManagementSystem.BLL.DTOs.PayOS;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface IPaymentService 
    {
        Task HandlePayOSWebhook(PayOSWebhookRequestDto webhookRequest);
    }
}
