using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.DAL.Repositories.Repository
{
    public class LiveStreamRepository : GenericRepository<Livestream>, ILiveStreamRepository
    {
        public LiveStreamRepository(CampEaseDatabaseContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Livestream>> GetLiveStreamsByDateRange(DateTime fromDate, DateTime toDate)
        {
            return await _context.ActivitySchedules
                .Where(a => a.startTime >= fromDate && a.endTime <= toDate && a.livestreamId != null)
                .Select(a => a.livestream)
                .Distinct()
                .ToListAsync();
        }
    }
}
