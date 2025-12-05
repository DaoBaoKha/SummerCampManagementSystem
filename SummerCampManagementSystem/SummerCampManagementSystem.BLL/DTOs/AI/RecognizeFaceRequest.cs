using Microsoft.AspNetCore.Http;

namespace SummerCampManagementSystem.BLL.DTOs.AI
{

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
        /// Camp ID (populated by controller from activity schedule)
        /// </summary>
        public int CampId { get; set; }

        /// <summary>
        /// Group ID for group-specific recognition (optional, for core activities)
        /// </summary>
        public int? GroupId { get; set; }
    }
}
