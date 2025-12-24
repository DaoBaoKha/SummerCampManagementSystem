using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;

namespace SummerCampManagementSystem.DAL.Repositories.Repository
{
    public class CamperAccommodationRepository : GenericRepository<CamperAccommodation>, ICamperAccommodationRepository
    {
        public CamperAccommodationRepository(CampEaseDatabaseContext context) : base(context)
        {
        }

        public async Task<IEnumerable<CamperAccommodation>> SearchAsync(int? camperId, int? accommodationId, int? campId, string? camperName)
        {
            var query = _context.Set<CamperAccommodation>()
                .Include(ca => ca.camper)
                .Include(ca => ca.accommodation)
                .ThenInclude(a => a.camp)
                .AsNoTracking();

            if (camperId.HasValue)
                query = query.Where(ca => ca.camperId == camperId.Value);

            if (accommodationId.HasValue)
                query = query.Where(ca => ca.accommodationId == accommodationId.Value);

            if (campId.HasValue)
                query = query.Where(ca => ca.accommodation.campId == campId.Value);

            if (!string.IsNullOrEmpty(camperName))
                query = query.Where(ca => ca.camper.camperName.Contains(camperName));

            // Filter out canceled campers
            var result = await query.ToListAsync();
            var camperIds = result.Select(ca => ca.camperId).Distinct().ToList();
            
            var canceledCamperIds = await _context.RegistrationCampers
                .Where(rc => camperIds.Contains(rc.camperId) && rc.status == "Canceled")
                .Select(rc => rc.camperId)
                .Distinct()
                .ToListAsync();

            return result.Where(ca => !canceledCamperIds.Contains(ca.camperId));
        }

        public async Task<CamperAccommodation?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.Set<CamperAccommodation>()
                .Include(ca => ca.camper)
                .Include(ca => ca.accommodation)
                    .ThenInclude(a => a.camp)
                .AsNoTracking()
                .FirstOrDefaultAsync(ca => ca.camperAccommodationId == id);
        }

        public async Task<CamperAccommodation?> GetByCamperAndAccommodationAsync(int camperId, int accommodationId)
        {
            return await _context.Set<CamperAccommodation>()
                .AsNoTracking()
                .FirstOrDefaultAsync(ca => ca.camperId == camperId && ca.accommodationId == accommodationId);
        }

        public async Task<CamperAccommodation?> GetByIdWithAccommodationAndCampAsync(int id)
        {
            return await _context.Set<CamperAccommodation>()
                .Include(ca => ca.camper)
                .Include(ca => ca.accommodation)
                    .ThenInclude(a => a.camp)
                .FirstOrDefaultAsync(ca => ca.camperAccommodationId == id);
        }

        public async Task<bool> IsAccommodationStaffOfCamper(int staffId, int camperId)
        {
            return await _context.Set<CamperAccommodation>()
                .AnyAsync(ca => ca.camperId == camperId
                             && ca.accommodation.supervisorId == staffId);
        }
    }
}
