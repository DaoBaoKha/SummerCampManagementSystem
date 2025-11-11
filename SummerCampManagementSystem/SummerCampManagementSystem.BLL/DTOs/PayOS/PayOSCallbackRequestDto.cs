namespace SummerCampManagementSystem.BLL.DTOs.PayOS
{
    public class PayOSCallbackRequestDto
    {
        // attribute PayOS send over Query Params
        public string Code { get; set; }
        public string Id { get; set; }
        public bool Cancel { get; set; }
        public string Status { get; set; }
        public long OrderCode { get; set; }
        public string Signature { get; set; }

        // receive deeplink which client mobile send to returnUrl
        public string? DeepLink { get; set; }
    }
}
