using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.BLL.DTOs.Camp;
using SummerCampManagementSystem.BLL.Helpers;
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
        private readonly IUserContextService _userContextService;

        public CampService(IUnitOfWork unitOfWork, IMapper mapper, IUserContextService userContextService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _userContextService = userContextService;
        }

        public async Task<CampResponseDto> CreateCampAsync(CampRequestDto campRequest)
        {
            // map the request DTO to the Camp entity
            var newCamp = _mapper.Map<Camp>(campRequest);

            // get userId
            newCamp.createBy = _userContextService.GetCurrentUserId();
            if (newCamp.createBy == null)
            {
                throw new UnauthorizedAccessException("Người dùng hiện tại không hợp lệ hoặc chưa đăng nhập.");
            }

            newCamp.status = CampStatus.Draft.ToString();

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

        public async Task<CampResponseDto> TransitionCampStatusAsync(int campId, CampStatus newStatus)
        {
            var existingCamp = await GetCampsWithIncludes()
                .FirstOrDefaultAsync(c => c.campId == campId);

            if (existingCamp == null)
            {
                throw new Exception($"Camp with ID {campId} not found.");
            }

            if (!Enum.TryParse(existingCamp.status, true, out CampStatus currentStatus))
            {
                throw new Exception("Trạng thái hiện tại của Camp không hợp lệ.");
            }

            bool isValidTransition = false;

            // can change status to Cancelled from Draft, PendingApproval, Rejected, Published, OpenForRegistration, RegistrationClosed.
            // cannot Cancelled from InProgress and Completed.
            if (newStatus == CampStatus.Canceled)
            {
                if (currentStatus != CampStatus.InProgress && currentStatus != CampStatus.Completed && currentStatus != CampStatus.Canceled)
                {
                    isValidTransition = true;
                }
            }
            else
            {
                switch (currentStatus)
                {
                    case CampStatus.Draft:
                        if (newStatus == CampStatus.PendingApproval)
                        {
                            isValidTransition = true;
                        }
                        break;

                    case CampStatus.PendingApproval:
                        if (newStatus == CampStatus.Published)
                        {
                            isValidTransition = true;
                        }
                        else if (newStatus == CampStatus.Rejected)
                        {
                            isValidTransition = true;
                        }
                        break;

                    case CampStatus.Rejected:
                        if (newStatus == CampStatus.PendingApproval)
                        {
                            isValidTransition = true;
                        }
                        break;

                    case CampStatus.Published:
                        if (newStatus == CampStatus.OpenForRegistration)
                        {
                            isValidTransition = true;
                        }
                        break;

                    case CampStatus.OpenForRegistration:
                        if (newStatus == CampStatus.RegistrationClosed)
                        {
                            isValidTransition = true;
                        }
                        break;

                    case CampStatus.RegistrationClosed:
                        if (newStatus == CampStatus.InProgress)
                        {
                            isValidTransition = true;
                        }
                        break;

                    case CampStatus.InProgress:
                        if (newStatus == CampStatus.Completed)
                        {
                            isValidTransition = true;
                        }
                        break;

                    case CampStatus.Completed:
                        break;
                }
            }

            if (!isValidTransition)
            {
                throw new ArgumentException($"Chuyển đổi trạng thái từ '{currentStatus}' sang '{newStatus}' không hợp lệ theo flow đã định.");
            }

            existingCamp.status = newStatus.ToString();

            await _unitOfWork.Camps.UpdateAsync(existingCamp);
            await _unitOfWork.CommitAsync();

            return _mapper.Map<CampResponseDto>(existingCamp);
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
            existingCamp.status = CampStatus.PendingApproval.ToString();

            await _unitOfWork.Camps.UpdateAsync(existingCamp);
            await _unitOfWork.CommitAsync();

            return _mapper.Map<CampResponseDto>(existingCamp);
        }

        public async Task<CampResponseDto> UpdateCampStatusAsync(int campId, CampStatusUpdateRequestDto statusUpdate)
        {
            return await TransitionCampStatusAsync(campId, statusUpdate.Status);
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
