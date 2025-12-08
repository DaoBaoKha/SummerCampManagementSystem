using Microsoft.AspNetCore.Http;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.DAL.UnitOfWork;
using Supabase;


namespace SummerCampManagementSystem.BLL.Services
{
    public class UploadSupabaseService : IUploadSupabaseService
    {
        private readonly Client _client;
        private readonly IUnitOfWork _unitOfWork;

        public UploadSupabaseService(Client client, IUnitOfWork unitOfWork)
        {
            _client = client;
            _unitOfWork = unitOfWork;
        }

        public async Task<string?> UploadCamperPhotoAsync(int camperId, IFormFile? file)
        {
            // Bucket: camper-photos (for registration before group assignment)
            // Path: {camperId}/filename
            return await UploadFileInternalAsync(file, "camper-photos", camperId.ToString());
        }

        public async Task<string?> UploadCamperPhotoToAttendanceAsync(int camperId, IFormFile? file)
        {
            // Bucket: attendance-sessions (when camper is enrolled in a group)
            // Path: campers/{camperId}/filename
            return await UploadFileInternalAsync(file, "attendance-sessions", $"campers/{camperId}");
        }

        public async Task<string?> UploadUserAvatarAsync(int userId, IFormFile? file)
        {
            // Bucket: user-avatars
            // Path: {userId}/filename
            return await UploadFileInternalAsync(file, "user-avatars", userId.ToString());
        }

        public async Task<string?> UploadDriverAvatarAsync(int userId, IFormFile? file)
        {
            // Bucket: driver-avatars
            // Path: {driverId}/filename
            return await UploadFileInternalAsync(file, "driver-avatars", userId.ToString());
        }

        public async Task<string?> UploadStaffAvatarAsync(int userId, IFormFile? file)
        {
            // Bucket: staff-avatars
            // Path: {staffId}/filename
            return await UploadFileInternalAsync(file, "staff-avatars", userId.ToString());
        }

        public async Task<string?> UploadBlogImageAsync(int blogId, IFormFile? file)
        {
            // Bucket: blog-images
            // Path: {blogId}/filename
            return await UploadFileInternalAsync(file, "blog-images", blogId.ToString());
        }

        public async Task<string?> UploadDriverLicensePhotoAsync(int userId, IFormFile? file)
        {
            // Bucket: driver-license-photos
            // Path: {driverId}/filename
            return await UploadFileInternalAsync(file, "driver-license-photos", userId.ToString());
        }

        public async Task<string?> UploadReportCamperAsync(int reportId, IFormFile? file)
        {
            // Bucket: report-camper-photos
            //Path : {reportId}/filename
            return await UploadFileInternalAsync(file, "report-camper-photos", reportId.ToString());
        }

        public async Task<string?> UploadRefundProofAsync(int registrationCancelId, IFormFile? file)
        {
            // Bucket: refund-proofs
            // Path: {registrationCancelId}/filename
            return await UploadFileInternalAsync(file, "refund-proofs", registrationCancelId.ToString());
        }


        public async Task<string?> UploadImage(IFormFile? file)
        {
            // Bucket: general-images
            // Path: filename
            return await UploadFileInternalAsync(file, "general-images", string.Empty);
        }

        #region Private Helper Method

        private async Task<string?> UploadFileInternalAsync(IFormFile? file, string bucketName, string folderPath)
        {
            if (file == null || file.Length == 0)
                return null;

            // 1. VALIDATE File Size (Max 5MB)
            const long maxFileSize = 5 * 1024 * 1024;
            if (file.Length > maxFileSize)
                throw new ArgumentException($"File size cannot exceed 5MB. Current size: {file.Length / 1024 / 1024}MB");

            // 2. VALIDATE File Extension
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(file.FileName).ToLower();

            if (!allowedExtensions.Contains(ext))
                throw new ArgumentException($"Only JPG, PNG, or WEBP images are allowed. Provided: {ext}");

            // 3. VALIDATE Content Type
            var allowedContentTypes = new[] { "image/jpeg", "image/png", "image/webp" };
            if (!allowedContentTypes.Contains(file.ContentType))
                throw new ArgumentException($"Invalid content type: {file.ContentType}");

            // 4. UPLOAD
            var storage = _client.Storage;
            var bucket = storage.From(bucketName);

            // Tạo tên file ngẫu nhiên để tránh trùng lặp
            var fileName = $"avatar_{Guid.NewGuid():N}{ext}";

            // Nếu có folderPath (ví dụ ID), ghép vào đường dẫn: "123/avatar_xyz.jpg"
            // Nếu không (Blog), chỉ dùng tên file: "avatar_xyz.jpg"
            var fullPath = string.IsNullOrEmpty(folderPath) ? fileName : $"{folderPath}/{fileName}";

            // 2. CHUYỂN IFormFile (Stream) SANG byte[]
            byte[] fileBytes;
            using (var stream = file.OpenReadStream())
            {
                // Đọc toàn bộ nội dung stream vào mảng byte
                fileBytes = new byte[file.Length];
                // Sử dụng ReadAsync/CopyToAsync để an toàn và hiệu quả hơn
                await stream.ReadAsync(fileBytes, 0, (int)file.Length);
            }

            await bucket.Upload(fileBytes, fullPath, new Supabase.Storage.FileOptions
            {
                ContentType = file.ContentType,
                Upsert = true // Ghi đè nếu file trùng tên (dù đã dùng GUID nhưng an toàn hơn)
            });

            // 5. RETURN Public URL
            return bucket.GetPublicUrl(fullPath);
        }

        #endregion
    }
}