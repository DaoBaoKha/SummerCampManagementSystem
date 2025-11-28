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


    }
}
