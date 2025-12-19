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
    public class AccommodationRepository : GenericRepository<Accommodation>, IAccommodationRepository
    {
        public AccommodationRepository(CampEaseDatabaseContext context) : base(context)
        {
            _context = context;
        }
        public async Task<IEnumerable<Accommodation>> GetByCampIdAsync(int campId)
        {
            return await _context.Accommodations
                .Where(a => a.campId == campId)
                .ToListAsync();
        }
        public async Task<Accommodation?> GetBySupervisorIdAsync(int supervisorId, int campId)
        {
            return await _context.Accommodations
                .Include(a => a.camp)
                .FirstOrDefaultAsync(a => a.supervisorId == supervisorId && a.campId == campId);
        }

        public async Task<bool> isSupervisorOfAccomodation(int supervisorId, int campId)
        {
            return await _context.Accommodations
                .AnyAsync(a => a.supervisorId == supervisorId && a.campId == campId);
                
        }

        public async Task<List<(string Name, int Current, int Max)>> GetCapacityAlertsByCampAsync(int campId)
        {
            var accommodations = await _context.Accommodations
                .Where(a => a.campId == campId && a.capacity.HasValue && a.capacity > 0)
                .Include(a => a.CamperAccommodations)
                .Select(a => new
                {
                    a.name,
                    Current = a.CamperAccommodations.Count,
                    Max = a.capacity.Value
                })
                .ToListAsync();

            // filter accommodation exceeding 80% capacity
            var alerts = accommodations
                .Where(a => (double)a.Current / a.Max >= 0.8)
                .Select(a => (a.name ?? "Unknown", a.Current, a.Max))
                .ToList();

            return alerts;
        }

        public async Task<List<int?>> GetAccommodationIdsWithSchedulesAsync(int campId)
        {
            // Logic: Join AccommodationActivity -> ActivitySchedule -> Activity -> Check CampId
            return await _context.AccommodationActivitySchedules
                .Include(aa => aa.activitySchedule).ThenInclude(s => s.activity)
                .Where(aa => aa.activitySchedule.activity.campId == campId)
                .Select(aa => aa.accommodationId)
                .Distinct()
                .ToListAsync();
        }

        public async Task<Accommodation?> GetByIdWithCamperAccommodationsAndCampAsync(int accommodationId)
        {
            return await _context.Accommodations
                .Include(a => a.CamperAccommodations)
                .Include(a => a.camp)
                .FirstOrDefaultAsync(a => a.accommodationId == accommodationId);
        }
    }
}
