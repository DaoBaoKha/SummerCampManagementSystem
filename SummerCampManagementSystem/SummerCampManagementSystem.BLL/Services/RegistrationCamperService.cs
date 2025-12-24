using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.BLL.DTOs.RegistrationCamper;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;
using SummerCampManagementSystem.BLL.Helpers;
using SummerCampManagementSystem.Core.Enums;
using SummerCampManagementSystem.BLL.Exceptions;

namespace SummerCampManagementSystem.BLL.Services
{
    public class RegistrationCamperService : IRegistrationCamperService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly CampEaseDatabaseContext _context;

        public RegistrationCamperService(IUnitOfWork unitOfWork, IMapper mapper, CampEaseDatabaseContext context)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _context = context;
        }

        public async Task<IEnumerable<RegistrationCamperResponseDto>> GetAllRegistrationCampersAsync()
        {

            var registrationCampers = await GetRegistrationCampersWithIncludes().ToListAsync();

            return _mapper.Map<IEnumerable<RegistrationCamperResponseDto>>(registrationCampers);
        }

        public async Task<IEnumerable<RegistrationCamperResponseDto>> SearchRegistrationCampersAsync(RegistrationCamperSearchDto searchDto)
        {
            IQueryable<RegistrationCamper> queryable = GetRegistrationCampersWithIncludes();

            if (searchDto.CamperId.HasValue)
            {
                queryable = queryable.Where(rc => rc.camperId == searchDto.CamperId.Value);
            }

            if (searchDto.CampId.HasValue)
            {
                queryable = queryable.Where(rc => rc.registration.campId == searchDto.CampId.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchDto.Status.ToString()))
            {
                string status = searchDto.Status.ToString().Trim().ToLower();
                queryable = queryable.Where(rc => rc.status.ToLower().Contains(status));
            }

            var filteredRegistrationCampers = await queryable.ToListAsync();

            return _mapper.Map<IEnumerable<RegistrationCamperResponseDto>>(filteredRegistrationCampers);
        }

        public async Task<RegistrationCamperResponseDto> LateCheckinAsync(LateCheckinRequestDto dto)
        {
            // Validate camp exists and get startDate
            var camp = await _unitOfWork.Camps.GetByIdAsync(dto.campId)
                ?? throw new NotFoundException($"Camp with ID {dto.campId} not found");

            if (!camp.startDate.HasValue)
            {
                throw new BadRequestException("Trại hè chưa có ngày bắt đầu");
            }

            // Validate camper exists and is registered for this camp
            var registrationCamper = await _unitOfWork.RegistrationCampers.GetByCamperIdAndCampIdAsync(dto.camperId, dto.campId);
            if (registrationCamper == null)
            {
                throw new BadRequestException($"Học viên {dto.camperId} chưa đăng ký trại hè {dto.campId}");
            }

            // Validate current time is within first day of camp
            //var currentTimeVn = TimezoneHelper.GetVietnamNow();
            //var campStartDateVn = camp.startDate.Value.ToVietnamTime();
            //var firstDayStart = campStartDateVn.Date; // 00:00:00
            //var firstDayEnd = campStartDateVn.Date.AddDays(1).AddSeconds(-1); // 23:59:59

            //if (currentTimeVn < firstDayStart || currentTimeVn > firstDayEnd)
            //{
            //    throw new BadRequestException($"Chỉ được phép check-in muộn trong ngày đầu tiên của trại ({campStartDateVn:dd/MM/yyyy}). Thời gian hiện tại: {currentTimeVn:dd/MM/yyyy HH:mm:ss}");
            //}

            // Validate current status allows check-in
            var validStatuses = new[] {
                RegistrationCamperStatus.Confirmed.ToString(),
                RegistrationCamperStatus.Transporting.ToString(),
                RegistrationCamperStatus.Transported.ToString()
            };

            if (!validStatuses.Contains(registrationCamper.status))
            {
                throw new BadRequestException($"Không thể check-in học viên với trạng thái hiện tại: {registrationCamper.status}. Trạng thái hợp lệ: Confirmed, Transporting, Transported");
            }

            // Update status to CheckedIn
            registrationCamper.status = RegistrationCamperStatus.CheckedIn.ToString();
            await _unitOfWork.RegistrationCampers.UpdateAsync(registrationCamper);
            await _unitOfWork.CommitAsync();

            // Return updated registration camper
            var updatedRegistrationCamper = await _unitOfWork.RegistrationCampers
                .GetByCompositeKeyWithIncludesAsync(registrationCamper.registrationId, registrationCamper.camperId);

            return _mapper.Map<RegistrationCamperResponseDto>(updatedRegistrationCamper);
        }

        #region Private Methods
        private IQueryable<RegistrationCamper> GetRegistrationCampersWithIncludes()
        {
            return _context.RegistrationCampers
                .AsSplitQuery() // <--- THÊM DÒNG NÀY
                .Include(rc => rc.registration)
                    .ThenInclude(r => r.camp) // load camp for campid
                 .Include(rc => rc.registration)
                    .ThenInclude(r => r.user)
                .Include(rc => rc.camper)    // load camper
                    .ThenInclude(c => c.CamperGroups) // load camper list
                        .ThenInclude(cg => cg.group)  // load group 
                            .ThenInclude(g => g.supervisor) // <--- THÊM DÒNG NÀY ĐỂ LẤY SUPERVISOR
                .Include(rc => rc.camper)    // load camper
                    .ThenInclude(c => c.CamperAccommodations)
                        .ThenInclude(ca => ca.accommodation)
                             .ThenInclude(g => g.supervisor); // <--- THÊM DÒNG NÀY ĐỂ LẤY SUPERVISOR
        }
        #endregion
    }
}
