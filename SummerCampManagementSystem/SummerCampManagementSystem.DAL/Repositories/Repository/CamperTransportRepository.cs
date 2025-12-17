using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;

namespace SummerCampManagementSystem.DAL.Repositories.Repository
{
    public class CamperTransportRepository : GenericRepository<CamperTransport>, ICamperTransportRepository
    {
        public CamperTransportRepository(CampEaseDatabaseContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<CamperTransport>> GetCamperTransportsByScheduleIdAsync(int scheduleId)
        {
            return await _context.CamperTransports
                .Where(ct => ct.transportScheduleId == scheduleId)
                .Include(ct => ct.camper)         
                .Include(ct => ct.stopLocation)   
                .ToListAsync();
        }

        public async Task<CamperTransport?> GetCamperTransportByScheduleAndCamperAsync(int transportScheduleId, int camperId)
        {
            return await _context.CamperTransports
                .Where(ct => ct.transportScheduleId == transportScheduleId && ct.camperId == camperId)
                .Include(ct => ct.camper)
                .Include(ct => ct.stopLocation)
                .Include(ct => ct.transportSchedule)
                .FirstOrDefaultAsync();
        }
    }
}
