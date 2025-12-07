using System.Text.Json.Serialization;

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
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        /// <summary>
        /// Status message from the Python AI service
        /// </summary>
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// The camp ID that was processed
        /// </summary>
        [JsonPropertyName("camp_id")]
        public int CampId { get; set; }

        /// <summary>
        /// Number of faces loaded/unloaded
        /// </summary>
        [JsonPropertyName("face_count")]
        public int FaceCount { get; set; }

        /// <summary>
        /// List of group IDs that were loaded
        /// </summary>
        [JsonPropertyName("groups")]
        public List<int>? Groups { get; set; }

        /// <summary>
        /// Timestamp of the operation
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTime? Timestamp { get; set; }
    }
}
