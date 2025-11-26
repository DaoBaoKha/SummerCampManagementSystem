using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface IUploadSupabaseService
    {
        Task<string?> UploadCamperPhotoAsync(int camperId, IFormFile? file);
        Task<string?> UploadCamperPhotoToAttendanceAsync(int camperId, IFormFile? file);
        Task<string?> UploadUserAvatarAsync(int userId, IFormFile? file);
        Task<string?> UploadDriverAvatarAsync(int driverId, IFormFile? file);
        Task<string?> UploadStaffAvatarAsync(int staffId, IFormFile? file);
        Task<string?> UploadBlogImageAsync(int blogId, IFormFile? file);
        Task<string?> UploadDriverLicensePhotoAsync(int userId, IFormFile? file);
        Task<string?> UploadReportCamperAsync(int reportId, IFormFile? file);
    }
}
