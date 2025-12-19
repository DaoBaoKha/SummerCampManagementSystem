using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SummerCampManagementSystem.BLL.DTOs.CamperAccommodation;
using SummerCampManagementSystem.BLL.DTOs.RegistrationCamper;
using SummerCampManagementSystem.BLL.Exceptions;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.Core.Enums;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;

namespace SummerCampManagementSystem.BLL.Services
{
    public class CamperAccommodationService : ICamperAccommodationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<CamperAccommodationService> _logger;

        public CamperAccommodationService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<CamperAccommodationService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<CamperAccommodationResponseDto>> GetCamperAccommodationsAsync(CamperAccommodationSearchDto searchDto)
        {
            var entities = await _unitOfWork.CamperAccommodations.SearchAsync(
                searchDto.CamperId,
                searchDto.AccommodationId,
                searchDto.CampId,
                searchDto.CamperName
            );

            return _mapper.Map<IEnumerable<CamperAccommodationResponseDto>>(entities);
        }

        public async Task<IEnumerable<RegistrationCamperResponseDto>> GetPendingAssignCampersAsync(int? campId)
        {
            // campers who are confirmed but not yet assigned to accommodation
            IQueryable<RegistrationCamper> query = _unitOfWork.RegistrationCampers.GetQueryable()
                .Where(rc => rc.status == RegistrationCamperStatus.Confirmed.ToString())
                .Include(rc => rc.camper)
                .Include(rc => rc.registration)
                    .ThenInclude(r => r.camp);

            if (campId.HasValue)
            {
                query = query.Where(rc => rc.registration.campId == campId.Value);
            }

            var entities = await query.ToListAsync();

            return _mapper.Map<IEnumerable<RegistrationCamperResponseDto>>(entities);
        }

        public async Task<CamperAccommodationResponseDto> CreateCamperAccommodationAsync(CamperAccommodationRequestDto requestDto)
        {
            if (!requestDto.camperId.HasValue || !requestDto.accommodationId.HasValue)
                throw new BadRequestException("CamperId và AccommodationId không được để trống.");

            var camper = await _unitOfWork.Campers.GetByIdAsync(requestDto.camperId.Value)
                ?? throw new NotFoundException($"Camper ID {requestDto.camperId} not found.");

            var accommodation = await _unitOfWork.Accommodations.GetByIdWithCamperAccommodationsAndCampAsync(requestDto.accommodationId.Value)
                ?? throw new NotFoundException($"Accommodation ID {requestDto.accommodationId} not found.");

            // validate camp hasn't started
            await ValidateCampNotStarted((int)accommodation.campId);

            // check if camper already in the accommodation
            var existing = await _unitOfWork.CamperAccommodations.GetByCamperAndAccommodationAsync(
                requestDto.camperId.Value,
                requestDto.accommodationId.Value
            );
            if (existing != null)
                throw new BusinessRuleException($"Camper {camper.camperName} đã được xếp vào chỗ ở {accommodation.name}.");

            // check camper status 
            var regCamper = await GetRegistrationCamperAsync(camper.camperId, (int)accommodation.campId);
            if (regCamper == null)
                throw new BusinessRuleException($"Camper chưa đăng ký hoặc chưa hoàn tất thanh toán cho Camp {accommodation.campId}.");

            if (regCamper.status != RegistrationCamperStatus.Confirmed.ToString())
            {
                throw new BusinessRuleException($"Không thể xếp chỗ ở. Trạng thái Camper là '{regCamper.status}'.");
            }

            // validate accommodation capacity
            int currentSize = accommodation.CamperAccommodations?.Count ?? 0;
            if (accommodation.capacity.HasValue && accommodation.capacity.Value > 0 && currentSize >= accommodation.capacity.Value)
            {
                throw new BusinessRuleException($"Không thể xếp chỗ ở. {accommodation.name} đã đầy (Sức chứa: {accommodation.capacity}).");
            }

            var camperAccommodation = new CamperAccommodation
            {
                camperId = requestDto.camperId.Value,
                accommodationId = requestDto.accommodationId.Value,
                status = CamperAccommodationStatus.Active.ToString()
            };

            await _unitOfWork.CamperAccommodations.CreateAsync(camperAccommodation);
            await _unitOfWork.CommitAsync();

            var createdEntity = await _unitOfWork.CamperAccommodations.GetByIdWithDetailsAsync(camperAccommodation.camperAccommodationId);

            return _mapper.Map<CamperAccommodationResponseDto>(createdEntity);
        }

        public async Task<CamperAccommodationResponseDto> UpdateCamperAccommodationAsync(int id, CamperAccommodationRequestDto requestDto)
        {
            if (!requestDto.accommodationId.HasValue)
                throw new BadRequestException("AccommodationId không được để trống khi cập nhật.");

            var currentMapping = await _unitOfWork.CamperAccommodations.GetByIdWithAccommodationAndCampAsync(id);

            if (currentMapping == null) throw new NotFoundException("Không tìm thấy thông tin CamperAccommodation.");

            var oldAccommodation = currentMapping.accommodation;

            // validate old camp hasn't started
            await ValidateCampNotStarted((int)oldAccommodation.campId);

            var newAccommodation = await _unitOfWork.Accommodations.GetByIdWithCamperAccommodationsAndCampAsync(requestDto.accommodationId.Value)
                ?? throw new NotFoundException("Không tìm thấy chỗ ở mới.");

            // validate new camp hasn't started
            await ValidateCampNotStarted((int)newAccommodation.campId);

            if (oldAccommodation.accommodationId == newAccommodation.accommodationId)
                throw new BusinessRuleException("Camper đã ở chỗ này rồi.");

            // validate new accommodation capacity
            int currentSize = newAccommodation.CamperAccommodations?.Count ?? 0;
            if (newAccommodation.capacity.HasValue && newAccommodation.capacity.Value > 0 && currentSize >= newAccommodation.capacity.Value)
            {
                throw new BusinessRuleException($"Không thể chuyển chỗ ở. {newAccommodation.name} đã đầy (Sức chứa: {newAccommodation.capacity}).");
            }

            currentMapping.accommodationId = newAccommodation.accommodationId;

            await _unitOfWork.CamperAccommodations.UpdateAsync(currentMapping);
            await _unitOfWork.CommitAsync();

            var updatedEntity = await _unitOfWork.CamperAccommodations.GetByIdWithDetailsAsync(id);

            return _mapper.Map<CamperAccommodationResponseDto>(updatedEntity);
        }

        public async Task<bool> DeleteCamperAccommodationAsync(int id)
        {
            var mapping = await _unitOfWork.CamperAccommodations.GetByIdWithAccommodationAndCampAsync(id);

            if (mapping == null) return false;

            var accommodation = mapping.accommodation;

            // validate camp hasn't started
            await ValidateCampNotStarted((int)accommodation.campId);

            // soft delete
            mapping.status = CamperAccommodationStatus.Inactive.ToString();

            await _unitOfWork.CamperAccommodations.UpdateAsync(mapping);
            await _unitOfWork.CommitAsync();

            return true;
        }

        #region Private Methods

        // validate camp hasn't started
        private async Task ValidateCampNotStarted(int campId)
        {
            var camp = await _unitOfWork.Camps.GetByIdAsync(campId)
                ?? throw new NotFoundException($"Camp with ID {campId} not found.");

            if (camp.startDate.HasValue && camp.startDate.Value <= DateTime.Now)
            {
                throw new BusinessRuleException(
                    $"Cannot assign/update/remove campers. Camp '{camp.name}' has already started on {camp.startDate.Value:yyyy-MM-dd}.");
            }
        }

        private async Task<RegistrationCamper?> GetRegistrationCamperAsync(int camperId, int campId)
        {
            return await _unitOfWork.RegistrationCampers.GetQueryable()
                .Include(rc => rc.registration)
                .FirstOrDefaultAsync(rc => rc.camperId == camperId && rc.registration.campId == campId);
        }

        #endregion
    }
}
