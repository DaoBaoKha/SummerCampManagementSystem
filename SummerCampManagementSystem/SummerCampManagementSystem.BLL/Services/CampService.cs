using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.BLL.DTOs.Requests.Camp;
using SummerCampManagementSystem.BLL.DTOs.Responses.Camp;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.Core.Enums;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;

namespace SummerCampManagementSystem.BLL.Services
{
    public class CampService : ICampService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public CampService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<CampResponseDto> CreateCampAsync(CampRequestDto campRequest)
        {
            // map the request DTO to the Camp entity
            var newCamp = _mapper.Map<Camp>(campRequest);

            // map create status draft
            newCamp.status = CampStatus.PendingApproval.ToString();

            await _unitOfWork.Camps.CreateAsync(newCamp);
            await _unitOfWork.CommitAsync();

            // get the created camp with related entities
            var createdCamp = await GetCampsWithIncludes()
                .FirstOrDefaultAsync(c => c.campId == newCamp.campId);

            if (createdCamp == null)
            {
                throw new Exception("Failed to retrieve the created camp for mapping.");
            }

            return _mapper.Map<CampResponseDto>(createdCamp);
        }

        public async Task<bool> DeleteCampAsync(int id)
        {
            var existingCamp = await _unitOfWork.Camps.GetByIdAsync(id);
            if (existingCamp == null) return false;
            await _unitOfWork.Camps.RemoveAsync(existingCamp);
            await _unitOfWork.CommitAsync();

            return true;
        }

        public async Task<IEnumerable<CampResponseDto>> GetAllCampsAsync()
        {
            var camps = await GetCampsWithIncludes().ToListAsync();

            return _mapper.Map<IEnumerable<CampResponseDto>>(camps);
        }

        public async Task<CampResponseDto?> GetCampByIdAsync(int id)
        {
            var camp = await GetCampsWithIncludes()
                .FirstOrDefaultAsync(c => c.campId == id);

            return camp == null ? null : _mapper.Map<CampResponseDto>(camp);
        }

        public async Task<IEnumerable<CampResponseDto>> GetCampsByTypeAsync(int campTypeId)
        {
            var camps = await GetCampsWithIncludes()
                .Where(c => c.campTypeId == campTypeId)
                .ToListAsync();

            if (camps == null || !camps.Any())
            {
                return Enumerable.Empty<CampResponseDto>();
            }

            return _mapper.Map<IEnumerable<CampResponseDto>>(camps);
        }

        public async Task<CampResponseDto> UpdateCampAsync(int campId, CampRequestDto campRequest)
        {
            // get the existing camp with related entities
            var existingCamp = await GetCampsWithIncludes()
                .FirstOrDefaultAsync(c => c.campId == campId);

            if (existingCamp == null)
            {
                throw new Exception("Camp not found");
            }

            _mapper.Map(campRequest, existingCamp);

            if (!string.IsNullOrEmpty(campRequest.Status))
            {

                // change from dto to enum
                if (Enum.TryParse(campRequest.Status, true, out CampStatus newStatus))
                {
                    existingCamp.status = newStatus.ToString();
                }
                else
                {
                    throw new ArgumentException($"Trạng thái '{campRequest.Status}' không hợp lệ.");
                }
            }

            await _unitOfWork.Camps.UpdateAsync(existingCamp);
            await _unitOfWork.CommitAsync();

            return _mapper.Map<CampResponseDto>(existingCamp);
        }

        public async Task<CampResponseDto> UpdateCampStatusAsync(int campId, CampStatusUpdateRequestDto statusUpdate)
        {
            var existingCamp = await GetCampsWithIncludes()
                .FirstOrDefaultAsync(c => c.campId == campId);

            if (existingCamp == null)
            {
                throw new Exception($"Camp with ID {campId} not found.");
            }

            // change enum to string
            existingCamp.status = statusUpdate.Status.ToString();

            await _unitOfWork.Camps.UpdateAsync(existingCamp);
            await _unitOfWork.CommitAsync();

            return _mapper.Map<CampResponseDto>(existingCamp);
        }

        public async Task<IEnumerable<CampResponseDto>> GetCampsByStatusAsync(CampStatus? status = null)
        {
            var query = GetCampsWithIncludes();

            if (status.HasValue)
            {
                string statusString = status.Value.ToString();

                query = query.Where(c => c.status == statusString);
            }
            //if status is null, return all camps

            var camps = await query.ToListAsync();

            return _mapper.Map<IEnumerable<CampResponseDto>>(camps);
        }

        // help method to include related entities
        private IQueryable<Camp> GetCampsWithIncludes()
        {
            //load related entities
            return _unitOfWork.Camps.GetQueryable()
                .Include(c => c.campType)
                .Include(c => c.location)
                .Include(c => c.promotion);
        }
    }
}
