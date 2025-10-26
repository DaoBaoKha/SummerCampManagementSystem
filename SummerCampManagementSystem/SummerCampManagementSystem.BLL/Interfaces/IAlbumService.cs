using SummerCampManagementSystem.BLL.DTOs.Album;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface IAlbumService
    {
        Task<AlbumResponseDto> CreateAlbumAsync(AlbumRequestDto albumRequest);
        Task<AlbumResponseDto?> GetAlbumByIdAsync(int id);
        Task<IEnumerable<AlbumResponseDto>> GetAllAlbumsAsync();
        Task<AlbumResponseDto> UpdateAlbumAsync(int id, AlbumRequestDto albumRequest);
        Task<bool> DeleteAlbumAsync(int id);

        Task<IEnumerable<AlbumResponseDto>> GetAlbumsByCampIdAsync(int campId);
    }
}
