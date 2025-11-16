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
    }
}
