using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.BLL.DTOs.AlbumPhoto;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;

namespace SummerCampManagementSystem.BLL.Services
{
    public class AlbumPhotoService : IAlbumPhotoService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public AlbumPhotoService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        private async Task RunValidationChecks(AlbumPhotoRequestDto photoRequest)
        {
            if (!photoRequest.AlbumId.HasValue)
            {
                throw new ArgumentException("Album ID is required.");
            }

            // check if album id exist
            var existingAlbum = await _unitOfWork.Albums.GetByIdAsync(photoRequest.AlbumId.Value);
            if (existingAlbum == null)
            {
                throw new KeyNotFoundException($"Album with ID {photoRequest.AlbumId.Value} not found. Cannot add photo.");
            }

            if (string.IsNullOrWhiteSpace(photoRequest.Photo))
            {
                throw new ArgumentException("Photo URL/path cannot be empty.");
            }
        }

        private IQueryable<AlbumPhoto> GetPhotosWithIncludes()
        {
            return _unitOfWork.AlbumPhotos.GetQueryable()
                .Include(ap => ap.album)
                .Include(ap => ap.AlbumPhotoFaces); 
        }

        public async Task<AlbumPhotoResponseDto> CreatePhotoAsync(AlbumPhotoRequestDto photoRequest)
        {
            await RunValidationChecks(photoRequest);

            var newPhoto = _mapper.Map<AlbumPhoto>(photoRequest);

            await _unitOfWork.AlbumPhotos.CreateAsync(newPhoto);
            await _unitOfWork.CommitAsync();

            var createdPhoto = await GetPhotosWithIncludes()
                .FirstOrDefaultAsync(ap => ap.albumPhotoId == newPhoto.albumPhotoId);

            return _mapper.Map<AlbumPhotoResponseDto>(createdPhoto);
        }

        public async Task<AlbumPhotoResponseDto?> GetPhotoByIdAsync(int id)
        {
            var photo = await GetPhotosWithIncludes()
                .FirstOrDefaultAsync(ap => ap.albumPhotoId == id);

            return photo == null ? null : _mapper.Map<AlbumPhotoResponseDto>(photo);
        }

        public async Task<IEnumerable<AlbumPhotoResponseDto>> GetPhotosByAlbumIdAsync(int albumId)
        {
            var photos = await GetPhotosWithIncludes()
                .Where(ap => ap.albumId == albumId)
                .ToListAsync();

            return _mapper.Map<IEnumerable<AlbumPhotoResponseDto>>(photos);
        }

        public async Task<AlbumPhotoResponseDto> UpdatePhotoAsync(int id, AlbumPhotoRequestDto photoRequest)
        {
            await RunValidationChecks(photoRequest);

            var existingPhoto = await GetPhotosWithIncludes()
                .FirstOrDefaultAsync(ap => ap.albumPhotoId == id);

            if (existingPhoto == null)
            {
                throw new KeyNotFoundException($"Photo with ID {id} not found.");
            }

            _mapper.Map(photoRequest, existingPhoto);

            await _unitOfWork.AlbumPhotos.UpdateAsync(existingPhoto);
            await _unitOfWork.CommitAsync();

            return _mapper.Map<AlbumPhotoResponseDto>(existingPhoto);
        }

        public async Task<bool> DeletePhotoAsync(int id)
        {
            var existingPhoto = await _unitOfWork.AlbumPhotos.GetByIdAsync(id);
            if (existingPhoto == null)
            {
                return false; 
            }

            // check if there r albumphotoface inside album photo
            var hasFaces = await _unitOfWork.AlbumPhotoFaces.GetQueryable()
                               .AnyAsync(f => f.albumPhotoId == id);

            if (hasFaces)
            {
                throw new InvalidOperationException("Cannot delete photo, it has associated face tags. Please remove tags first.");

                // Hoặc nếu bạn muốn xóa luôn cả tags (cần logic xóa tags trước):
                // await _unitOfWork.AlbumPhotoFaces.RemoveRangeAsync(...);
            }

            await _unitOfWork.AlbumPhotos.RemoveAsync(existingPhoto);
            await _unitOfWork.CommitAsync();

            return true;
        }
    }
}