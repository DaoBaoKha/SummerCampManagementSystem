namespace SummerCampManagementSystem.BLL.DTOs.AI
{
    /// <summary>
    /// Request DTO for loading camp face database into Python AI service
    /// </summary>
    public class LoadFaceDbRequest
    {
        /// <summary>
        /// The camp ID to load face database for
        /// </summary>
        public int CampId { get; set; }

        /// <summary>
        /// Optional: Force reload even if already loaded
        /// </summary>
        public bool ForceReload { get; set; } = false;
    }
}
