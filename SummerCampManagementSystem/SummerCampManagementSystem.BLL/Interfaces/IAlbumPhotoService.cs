using SummerCampManagementSystem.BLL.DTOs.AlbumPhoto;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface IAlbumPhotoService
    {
        Task<AlbumPhotoResponseDto> CreatePhotoAsync(AlbumPhotoRequestDto photoRequest);
        Task<AlbumPhotoResponseDto?> GetPhotoByIdAsync(int id);
        Task<IEnumerable<AlbumPhotoResponseDto>> GetPhotosByAlbumIdAsync(int albumId);
        Task<AlbumPhotoResponseDto> UpdatePhotoAsync(int id, AlbumPhotoRequestDto photoRequest);
        Task<bool> DeletePhotoAsync(int id);
    }
}
