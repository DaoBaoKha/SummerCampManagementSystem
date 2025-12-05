using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;

namespace SummerCampManagementSystem.DAL.Repositories.Repository
{
    public class TransportScheduleRepository : GenericRepository<TransportSchedule>, ITransportScheduleRepository
    {
        public TransportScheduleRepository(CampEaseDatabaseContext context) : base(context)
        {
            _context = context;
        }

        public IQueryable<TransportSchedule> GetSchedulesWithIncludes()
        {
            return _context.TransportSchedules
                .Include(s => s.camp)
                .Include(s => s.route)
                .Include(s => s.vehicle)
                .Include(s => s.driver).ThenInclude(d => d.user);
        }

        public async Task<IEnumerable<TransportSchedule>> GetSchedulesByCamperIdAsync(int camperId)
        {
            // query from CamperTransport 
            return await _context.CamperTransports
                .Where(ct => ct.camperId == camperId)
                // get detail transportSchedule
                .Include(ct => ct.transportSchedule).ThenInclude(ts => ts.route)
                .Include(ct => ct.transportSchedule).ThenInclude(ts => ts.vehicle)
                .Include(ct => ct.transportSchedule).ThenInclude(ts => ts.driver).ThenInclude(d => d.user)
                .Include(ct => ct.transportSchedule).ThenInclude(ts => ts.camp)
                .Select(ct => ct.transportSchedule)
                .OrderByDescending(ts => ts.date) // recent date on top
                .ToListAsync();
        }
    }
}