namespace SummerCampManagementSystem.BLL.DTOs.AI
{
    /// <summary>
    /// Request DTO for unloading camp face database from Python AI service
    /// </summary>
    public class UnloadFaceDbRequest
    {
        /// <summary>
        /// The camp ID to unload face database for
        /// </summary>
        public int CampId { get; set; }
    }
}
