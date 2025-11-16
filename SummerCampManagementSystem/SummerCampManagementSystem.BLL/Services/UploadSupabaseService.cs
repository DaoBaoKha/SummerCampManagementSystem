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

            var storage = _client.Storage;
            var bucket = storage.From("camper-photos");

            var fileName = "avatar" + Path.GetExtension(file.FileName);
            var path = $"{camperId}/{fileName}";

            // using var stream = file.OpenReadStream();

            // 2. CHUYỂN IFormFile (Stream) SANG byte[]
            byte[] fileBytes;
            using (var stream = file.OpenReadStream())
            {
                // Đọc toàn bộ nội dung stream vào mảng byte
                fileBytes = new byte[file.Length];
                // Sử dụng ReadAsync/CopyToAsync để an toàn và hiệu quả hơn
                await stream.ReadAsync(fileBytes, 0, (int)file.Length);
            }

            await bucket.Upload(fileBytes, path, new Supabase.Storage.FileOptions
            {
                ContentType = file.ContentType,
                Upsert = true
            });

            return bucket.GetPublicUrl(path);
        }

    }
}
