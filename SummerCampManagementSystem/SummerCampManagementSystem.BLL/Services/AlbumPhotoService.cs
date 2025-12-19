using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.BLL.DTOs.AlbumPhoto;
using SummerCampManagementSystem.BLL.Exceptions;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;

namespace SummerCampManagementSystem.BLL.Services
{
    public class AlbumPhotoService : IAlbumPhotoService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IUploadSupabaseService _uploadService;

        public AlbumPhotoService(IUnitOfWork unitOfWork, IMapper mapper, IUploadSupabaseService uploadService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _uploadService = uploadService;
        }

        #region Private Methods

        private async Task RunValidationChecks(AlbumPhotoRequestDto photoRequest)
        {
            if (!photoRequest.AlbumId.HasValue)
            {
                throw new BadRequestException("Album ID is required.");
            }

            // VALIDATE: Album exists
            var existingAlbum = await _unitOfWork.Albums.GetByIdAsync(photoRequest.AlbumId.Value);
            if (existingAlbum == null)
            {
                throw new NotFoundException($"Album with ID {photoRequest.AlbumId.Value} not found. Cannot add photo.");
            }

            if (string.IsNullOrWhiteSpace(photoRequest.Photo))
            {
                throw new BadRequestException("Photo URL/path cannot be empty.");
            }
        }

        private IQueryable<AlbumPhoto> GetPhotosWithIncludes()
        {
            return _unitOfWork.AlbumPhotos.GetQueryable()
                .Include(ap => ap.album)
                .Include(ap => ap.AlbumPhotoFaces); 
        }

        #endregion

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
                throw new NotFoundException($"Photo with ID {id} not found.");
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

            // VALIDATE: Check if there are albumPhotoFaces inside album photo
            var hasFaces = await _unitOfWork.AlbumPhotoFaces.GetQueryable()
                               .AnyAsync(f => f.albumPhotoId == id);

            if (hasFaces)
            {
                throw new BusinessRuleException("Cannot delete photo, it has associated face tags. Please remove tags first.");
            }

            await _unitOfWork.AlbumPhotos.RemoveAsync(existingPhoto);
            await _unitOfWork.CommitAsync();

            return true;
        }

        public async Task<BulkAlbumPhotoResponseDto> CreateMultiplePhotosAsync(
            int albumId, 
            List<Microsoft.AspNetCore.Http.IFormFile> photos, 
            string? defaultCaption = null)
        {
            // VALIDATE: Album exists
            var existingAlbum = await _unitOfWork.Albums.GetByIdAsync(albumId);
            if (existingAlbum == null)
            {
                throw new NotFoundException($"Album with ID {albumId} not found.");
            }

            var response = new BulkAlbumPhotoResponseDto();

            // UPLOAD: Upload all photos to Supabase first
            List<string> uploadedUrls;
            try
            {
                uploadedUrls = await _uploadService.UploadMultipleAlbumPhotosAsync(albumId, photos);
            }
            catch (BadRequestException ex)
            {
                throw new BadRequestException($"Bulk upload validation failed: {ex.Message}");
            }

            // CREATE: Create AlbumPhoto records for each uploaded URL
            for (int i = 0; i < uploadedUrls.Count; i++)
            {
                try
                {
                    var photoRequest = new AlbumPhotoRequestDto
                    {
                        AlbumId = albumId,
                        Photo = uploadedUrls[i],
                        Caption = defaultCaption ?? photos[i].FileName
                    };

                    var newPhoto = _mapper.Map<AlbumPhoto>(photoRequest);
                    await _unitOfWork.AlbumPhotos.CreateAsync(newPhoto);
                    await _unitOfWork.CommitAsync();

                    var createdPhoto = await GetPhotosWithIncludes()
                        .FirstOrDefaultAsync(ap => ap.albumPhotoId == newPhoto.albumPhotoId);

                    var responseDto = _mapper.Map<AlbumPhotoResponseDto>(createdPhoto);
                    response.SuccessfulPhotos.Add(responseDto);
                    response.SuccessCount++;
                }
                catch (Exception ex)
                {
                    response.Errors.Add(new BulkUploadError
                    {
                        FileName = photos[i].FileName,
                        ErrorMessage = ex.Message
                    });
                    response.FailureCount++;
                }
            }

            return response;
        }
    }
}