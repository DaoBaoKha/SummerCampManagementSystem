using Microsoft.AspNetCore.Http;
using SummerCampManagementSystem.BLL.Exceptions;
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

        public async Task<string?> UploadAlbumPhotoAsync(int albumId, IFormFile? file)
        {
            // Bucket: album-photos
            // Path: {albumId}/filename
            return await UploadFileInternalAsync(file, "album-photos", albumId.ToString());
        }

        public async Task<List<string>> UploadMultipleAlbumPhotosAsync(int albumId, List<IFormFile> files)
        {
            // validate only 20 files per upload
            const int maxFilesPerUpload = 20;
            if (files.Count > maxFilesPerUpload)
            {
                throw new BadRequestException($"Maximum {maxFilesPerUpload} files allowed per upload. Received: {files.Count}");
            }

            // validate total size not exceeding 50MB
            const long maxTotalSize = 50 * 1024 * 1024; // 50MB
            var totalSize = files.Sum(f => f.Length);
            if (totalSize > maxTotalSize)
            {
                throw new BadRequestException($"Total file size cannot exceed 50MB. Current total: {totalSize / 1024 / 1024}MB");
            }

            var uploadedUrls = new List<string>();

            foreach (var file in files)
            {
                var url = await UploadAlbumPhotoAsync(albumId, file);
                if (!string.IsNullOrEmpty(url))
                {
                    uploadedUrls.Add(url);
                }
            }

            return uploadedUrls;
        }

        #region Private Helper Method

        private async Task<string?> UploadFileInternalAsync(IFormFile? file, string bucketName, string folderPath)
        {
            if (file == null || file.Length == 0)
                return null;

            // validate file size (max 5MB)
            const long maxFileSize = 5 * 1024 * 1024;
            if (file.Length > maxFileSize)
                throw new BadRequestException($"File size cannot exceed 5MB. Current size: {file.Length / 1024 / 1024}MB");

            // validate file extension
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(file.FileName).ToLower();

            if (!allowedExtensions.Contains(ext))
                throw new BadRequestException($"Only JPG, PNG, or WEBP images are allowed. Provided: {ext}");

            // validate content type
            var allowedContentTypes = new[] { "image/jpeg", "image/png", "image/webp" };
            if (!allowedContentTypes.Contains(file.ContentType))
                throw new BadRequestException($"Invalid content type: {file.ContentType}");

            var storage = _client.Storage;
            var bucket = storage.From(bucketName);

            // generate filename 
            var fileName = $"avatar_{Guid.NewGuid():N}{ext}";

            // build path "123/avatar_xyz.jpg" or just "avatar_xyz.jpg"
            var fullPath = string.IsNullOrEmpty(folderPath) ? fileName : $"{folderPath}/{fileName}";

            // convert IFormFile (stream) to byte[]
            byte[] fileBytes;
            using (var stream = file.OpenReadStream())
            {
                fileBytes = new byte[file.Length];
                await stream.ReadAsync(fileBytes, 0, (int)file.Length);
            }

            await bucket.Upload(fileBytes, fullPath, new Supabase.Storage.FileOptions
            {
                ContentType = file.ContentType,
                Upsert = true // overwrite if file name already exists
            });

            return bucket.GetPublicUrl(fullPath);
        }

        #endregion
    }
}