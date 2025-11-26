namespace SummerCampManagementSystem.BLL.DTOs.AI
{
    /// <summary>
    /// Response DTO for face database load/unload operations
    /// </summary>
    public class FaceDbResponse
    {
        /// <summary>
        /// Whether the operation was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Status message from the Python AI service
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// The camp ID that was processed
        /// </summary>
        public int CampId { get; set; }

        /// <summary>
        /// Number of faces loaded/unloaded
        /// </summary>
        public int FaceCount { get; set; }

        /// <summary>
        /// Timestamp of the operation
        /// </summary>
        public DateTime Timestamp { get; set; }
    }
}
