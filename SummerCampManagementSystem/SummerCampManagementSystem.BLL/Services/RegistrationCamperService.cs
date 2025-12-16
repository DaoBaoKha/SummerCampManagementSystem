using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.BLL.DTOs.RegistrationCamper;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;

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
