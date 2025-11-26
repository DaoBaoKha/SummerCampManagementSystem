using SummerCampManagementSystem.BLL.DTOs.AI;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    /// <summary>
    /// Interface for communicating with Python Flask AI face recognition service
    /// </summary>
    public interface IPythonAiService
    {
        /// <summary>
        /// Health check for the Python AI service
        /// </summary>
        /// <returns>Health status of the AI service</returns>
        Task<HealthCheckResponse> HealthCheckAsync();

        /// <summary>
        /// Load face database for a specific camp into Python AI service memory
        /// Downloads faces from Supabase attendance-sessions bucket and loads them into DeepFace model
        /// </summary>
        /// <param name="campId">The camp ID to load face database for</param>
        /// <param name="forceReload">Force reload even if already loaded</param>
        /// <returns>Response containing success status and face count</returns>
        Task<FaceDbResponse> LoadCampFaceDbAsync(int campId, bool forceReload = false);

        /// <summary>
        /// Unload face database for a specific camp from Python AI service memory
        /// Clears camp data from memory and optionally deletes local files
        /// </summary>
        /// <param name="campId">The camp ID to unload face database for</param>
        /// <returns>Response containing success status</returns>
        Task<FaceDbResponse> UnloadCampFaceDbAsync(int campId);

        /// <summary>
        /// Recognize faces in a photo for a specific activity schedule
        /// Processes the photo using DeepFace model and returns recognized campers
        /// </summary>
        /// <param name="request">Recognition request containing activity schedule ID and photo</param>
        /// <returns>Response containing list of recognized campers with confidence scores</returns>
        Task<RecognitionResponse> RecognizeAsync(RecognizeFaceRequest request);

        /// <summary>
        /// Get statistics about loaded camps in Python AI service
        /// </summary>
        /// <returns>Dictionary with camp IDs as keys and face counts as values</returns>
        Task<Dictionary<int, int>> GetLoadedCampsAsync();
    }
}
