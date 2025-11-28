using Microsoft.AspNetCore.Http;

namespace SummerCampManagementSystem.BLL.DTOs.AI
{
    /// <summary>
    /// Request DTO for recognizing faces in an activity schedule photo
    /// </summary>
    /// <remarks>
    /// Confidence threshold is configured in Python service .env file (single source of truth)
    /// </remarks>
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
    }
}
