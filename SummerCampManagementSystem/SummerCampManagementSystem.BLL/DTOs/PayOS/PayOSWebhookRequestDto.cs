namespace SummerCampManagementSystem.BLL.DTOs.PayOS
{
    public class PayOSWebhookRequestDto
    {
        public string code { get; set; }
        public string desc { get; set; }
        public bool success { get; set; }
        public PayOSWebhookDataDto data { get; set; }
        public string signature { get; set; }
    }
}
