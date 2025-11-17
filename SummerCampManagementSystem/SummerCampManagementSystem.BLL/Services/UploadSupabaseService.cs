using Microsoft.AspNetCore.Http;
using SummerCampManagementSystem.BLL.Interfaces;
using Supabase;


namespace SummerCampManagementSystem.BLL.Services
{
    public class UploadSupabaseService : IUploadSupabaseService
    {
        private readonly Client _client;

        public UploadSupabaseService(Client client)
        {
            _client = client;
        }

        public async Task<string?> UploadCamperPhotoAsync(int camperId, IFormFile? file)
        {
            if (file == null)
                return null;

            // VALIDATE 1 — File size (giới hạn 5MB)
            const long maxFileSize = 5 * 1024 * 1024; // 5MB
            if (file.Length == 0)
                throw new ArgumentException("File upload is empty.");

            if (file.Length > maxFileSize)
                throw new ArgumentException("File size cannot exceed 5MB.");

            // VALIDATE 2 — Extension hợp lệ
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(file.FileName).ToLower();

            if (!allowedExtensions.Contains(ext))
                throw new ArgumentException("Only JPG, PNG, or WEBP images are allowed.");

            // VALIDATE 3 — Content-Type
            var allowedContentTypes = new[] { "image/jpeg", "image/png", "image/webp" };
            if (!allowedContentTypes.Contains(file.ContentType))
                throw new ArgumentException("Invalid image file format.");

            // VALIDATE OK → tiếp tục upload
            var storage = _client.Storage;
            var bucket = storage.From("camper-photos");

            // File name an toàn để tránh bị overwrite
            var fileName = $"avatar_{Guid.NewGuid():N}{ext}";
            var path = $"{camperId}/{fileName}";

            // Convert IFormFile → byte[]
            byte[] fileBytes;
            using (var stream = file.OpenReadStream())
            {
                fileBytes = new byte[file.Length];
                await stream.ReadAsync(fileBytes, 0, (int)file.Length);
            }

            // Upload
            await bucket.Upload(fileBytes, path, new Supabase.Storage.FileOptions
            {
                ContentType = file.ContentType,
                Upsert = true
            });

            // Trả về public URL
            return bucket.GetPublicUrl(path);
        }

    }
}
