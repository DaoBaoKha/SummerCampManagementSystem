using Microsoft.AspNetCore.Http;

namespace SummerCampManagementSystem.BLL.DTOs.AI
{
    /// <summary>
    /// Request DTO for recognizing faces in an activity schedule photo
    /// </summary>
    public class RecognizeFaceRequest
    {
        /// <summary>
        /// The activity schedule ID for the recognition session
        /// </summary>
        public int ActivityScheduleId { get; set; }

        /// <summary>
        /// The photo file containing faces to recognize
        /// </summary>
        public IFormFile Photo { get; set; } = null!;

        /// <summary>
        /// Optional: Confidence threshold override (0.0 - 1.0)
        /// If not provided, uses default from configuration
        /// </summary>
        public float? ConfidenceThreshold { get; set; }
    }
}
