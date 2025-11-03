namespace SummerCampManagementSystem.BLL.Helpers
{
    public class GeminiApiSettings
    {
        public const string SectionName = "GeminiApi";
        public string ApiKey { get; set; }
        public string ApiBaseUrl { get; set; }
        public string ModelName { get; set; }
    }
}
