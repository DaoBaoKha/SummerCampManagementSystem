namespace SummerCampManagementSystem.BLL.DTOs;

/// <summary>
/// Webhook request from Python face recognition service
/// </summary>
public class RecognitionWebhookRequest
{
    public string RequestId { get; set; } = string.Empty;
    public int ActivityScheduleId { get; set; }
    public int GroupId { get; set; }
    public int CampId { get; set; }
    public List<RecognizedFaceDto> RecognizedFaces { get; set; } = new();
    public WebhookMetadata Metadata { get; set; } = new();
}

/// <summary>
/// Recognized face with embedding and bounding box
/// </summary>
public class RecognizedFaceDto
{
    public List<double> Embedding { get; set; } = new();
    public double Confidence { get; set; }
    public BoundingBoxDto? BoundingBox { get; set; }
    public int FaceArea { get; set; }
    public int? CamperId { get; set; }  // Optional: Python may pre-match
}

/// <summary>
/// Face bounding box coordinates
/// </summary>
public class BoundingBoxDto
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}

/// <summary>
/// Webhook metadata (timestamp, user info)
/// </summary>
public class WebhookMetadata
{
    public DateTime Timestamp { get; set; }
    public string ProcessedBy { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Source { get; set; } = "mobile-direct";
    public string PythonVersion { get; set; } = string.Empty;
}

/// <summary>
/// Matched camper after embedding comparison
/// </summary>
public class RecognizedCamperDto
{
    public int CamperId { get; set; }
    public string CamperName { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public double Distance { get; set; }
    public BoundingBoxDto? BoundingBox { get; set; }
}

/// <summary>
/// Result of attendance update operation
/// </summary>
public class AttendanceUpdateResult
{
    public bool Success { get; set; }
    public int UpdatedCount { get; set; }
    public int CreatedCount { get; set; }
    public List<RecognizedCamperDto> RecognizedCampers { get; set; } = new();
    public DateTime Timestamp { get; set; }
    public string ProcessedBy { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public bool BroadcastSent { get; set; }
}
