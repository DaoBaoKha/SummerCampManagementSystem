namespace SummerCampManagementSystem.BLL.DTOs.AI
{
    /// <summary>
    /// Represents a recognized camper from face recognition
    /// </summary>
    public class RecognizedCamper
    {
        /// <summary>
        /// The camper ID that was recognized
        /// </summary>
        public int CamperId { get; set; }

        /// <summary>
        /// Full name of the camper
        /// </summary>
        public string CamperName { get; set; } = string.Empty;

        /// <summary>
        /// Confidence score of the recognition (0.0 - 1.0)
        /// </summary>
        public float Confidence { get; set; }

        /// <summary>
        /// Camper group ID the camper belongs to
        /// </summary>
        public int CamperGroupId { get; set; }

        /// <summary>
        /// Bounding box coordinates in the photo [x, y, width, height]
        /// </summary>
        public int[]? BoundingBox { get; set; }
    }
}
