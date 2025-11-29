namespace SummerCampManagementSystem.BLL.DTOs.AI
{
    /// <summary>
    /// Response DTO for AI service health check
    /// </summary>
    public class HealthCheckResponse
    {
        /// <summary>
        /// Whether the AI service is healthy
        /// </summary>
        public bool IsHealthy { get; set; }

        /// <summary>
        /// Status message
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Model name being used
        /// </summary>
        public string ModelName { get; set; } = string.Empty;

        /// <summary>
        /// Number of camps currently loaded in memory
        /// </summary>
        public int LoadedCamps { get; set; }

        /// <summary>
        /// Timestamp of the health check
        /// </summary>
        public DateTime Timestamp { get; set; }
    }
}
