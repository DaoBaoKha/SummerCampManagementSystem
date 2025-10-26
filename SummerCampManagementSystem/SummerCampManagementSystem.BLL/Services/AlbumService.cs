using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.BLL.DTOs.Album;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;
using AutoMapper; 

namespace SummerCampManagementSystem.BLL.Services
{
    public class AlbumService : IAlbumService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper; 

        public AlbumService(IUnitOfWork unitOfWork, IMapper mapper) 
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }


        private async Task RunValidationChecks(AlbumRequestDto albumRequest)
        {
            if (!albumRequest.CampId.HasValue)
            {
                throw new ArgumentException("Camp ID is required and must be a positive integer.");
            }

            var existingCamp = await _unitOfWork.Camps.GetByIdAsync(albumRequest.CampId.Value);
            if (existingCamp == null)
            {
                throw new KeyNotFoundException($"Camp with ID {albumRequest.CampId.Value} not found. Cannot link album.");
            }

            if (string.IsNullOrWhiteSpace(albumRequest.Title))
            {
                throw new ArgumentException("Album title cannot be empty.");
            }
        }

        private IQueryable<Album> GetAlbumsWithIncludes()
        {
            return _unitOfWork.Albums.GetQueryable()
                .Include(a => a.camp)
                .Include(a => a.AlbumPhotos);
        }

        public async Task<AlbumResponseDto> CreateAlbumAsync(AlbumRequestDto albumRequest)
        {
            await RunValidationChecks(albumRequest);

            var newAlbum = _mapper.Map<Album>(albumRequest);

            if (!newAlbum.date.HasValue)
            {
                newAlbum.date = DateOnly.FromDateTime(DateTime.Today);
            }

            await _unitOfWork.Albums.CreateAsync(newAlbum);
            await _unitOfWork.CommitAsync();

            var createdAlbum = await GetAlbumsWithIncludes()
                .FirstOrDefaultAsync(a => a.albumId == newAlbum.albumId);

            return _mapper.Map<AlbumResponseDto>(createdAlbum);
        }

        public async Task<AlbumResponseDto?> GetAlbumByIdAsync(int id)
        {
            var album = await GetAlbumsWithIncludes()
                .FirstOrDefaultAsync(a => a.albumId == id);

            return album == null ? null : _mapper.Map<AlbumResponseDto>(album);
        }

        public async Task<IEnumerable<AlbumResponseDto>> GetAllAlbumsAsync()
        {
            var albums = await GetAlbumsWithIncludes().ToListAsync();

            return _mapper.Map<IEnumerable<AlbumResponseDto>>(albums);
        }

        public async Task<IEnumerable<AlbumResponseDto>> GetAlbumsByCampIdAsync(int campId)
        {
            var albums = await GetAlbumsWithIncludes()
                .Where(a => a.campId == campId)
                .ToListAsync();

            return _mapper.Map<IEnumerable<AlbumResponseDto>>(albums);
        }

        public async Task<AlbumResponseDto> UpdateAlbumAsync(int id, AlbumRequestDto albumRequest)
        {
            await RunValidationChecks(albumRequest);

            var existingAlbum = await GetAlbumsWithIncludes().FirstOrDefaultAsync(a => a.albumId == id);

            if (existingAlbum == null)
            {
                throw new KeyNotFoundException($"Album with ID {id} not found.");
            }

            _mapper.Map(albumRequest, existingAlbum);


            await _unitOfWork.Albums.UpdateAsync(existingAlbum);
            await _unitOfWork.CommitAsync();

            return _mapper.Map<AlbumResponseDto>(existingAlbum);
        }

        public async Task<bool> DeleteAlbumAsync(int id)
        {
            var existingAlbum = await _unitOfWork.Albums.GetByIdAsync(id);
            if (existingAlbum == null) return false;

            // cannot delete if photos are still in there
            var hasPhotos = await _unitOfWork.AlbumPhotos.GetQueryable()
                            .AnyAsync(ap => ap.albumId == id);

            if (hasPhotos)
            {
                throw new ArgumentException("Cannot delete album because it still contains photos. Please delete all photos first.");
            }

            await _unitOfWork.Albums.RemoveAsync(existingAlbum);
            await _unitOfWork.CommitAsync();

            return true;
        }
    }
}
