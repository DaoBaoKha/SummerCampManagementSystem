using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;

namespace SummerCampManagementSystem.DAL.Repositories.Repository
{
    public class CampRepository : GenericRepository<Camp>, ICampRepository
    {
        public CampRepository(CampEaseDatabaseContext context) : base(context)
        {
        }
        public async Task<IEnumerable<Camp>> GetCampsByTypeAsync(int campTypeId)
        {
            return await _context.Camps.Where(c => c.campTypeId == campTypeId).ToListAsync();
        }

        public async Task<IEnumerable<Camp>> GetCampsByStaffIdAsync(int staffId)
        {
            var activityCampIds = await _context.ActivitySchedules
                .Where(a => a.staffId == staffId)
                .Select(a => a.activity.campId)
                .ToListAsync();

            var groupCampIds = await _context.Groups
                .Where(g => g.supervisorId == staffId) 
                .Select(g => g.campId)
                .ToListAsync();

            var accommodationCampIds = await _context.Accommodations
                .Where(a => a.supervisorId == staffId)
                .Select(a => a.campId)
                .ToListAsync();

            var allCampIds = activityCampIds
                .Concat(groupCampIds)
                .Concat(accommodationCampIds)
                .Distinct()
                .ToList();

            var allCamps = await _context.Camps
                .Where(c => allCampIds.Contains(c.campId))
                .ToListAsync();

            return allCamps;
        }

        // ADMIN DASHBOARD METHODS
        public async Task<int> GetActiveCampsCountAsync()
        {
            // active camps = all camps except Draft, PendingApproval, Rejected, UnderEnrolled, Canceled
            var inactiveStatuses = new[] { "Draft", "PendingApproval", "Rejected", "UnderEnrolled", "Canceled" };
            
            return await _context.Camps
                .Where(c => !inactiveStatuses.Contains(c.status))
                .CountAsync();
        }

        public async Task<Dictionary<string, int>> GetCampStatusDistributionAsync()
        {
            var distribution = await _context.Camps
                .GroupBy(c => c.status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            return distribution.ToDictionary(x => x.Status, x => x.Count);
        }

        public async Task<List<(string Month, decimal Revenue)>> GetMonthlyRevenueAsync(int months)
        {
            var startDate = DateTime.UtcNow.Date.AddMonths(-months);

            var monthlyRevenue = await _context.Transactions
                .Where(t => t.transactionTime.HasValue && 
                           t.transactionTime >= startDate &&
                           (t.status == "Confirmed" || t.status == "Refunded"))
                .GroupBy(t => new { Year = t.transactionTime.Value.Year, Month = t.transactionTime.Value.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Revenue = g.Where(t => t.status == "Confirmed").Sum(t => t.amount ?? 0) -
                             g.Where(t => t.status == "Refunded").Sum(t => t.amount ?? 0)
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToListAsync();

            return monthlyRevenue.Select(x => ($"{x.Month:D2}/{x.Year}", x.Revenue)).ToList();
        }

        public async Task<List<(int CampId, string Name, string ManagerName, DateTime SubmittedDate, string Status)>> GetPendingCampsAsync()
        {
            var pendingCamps = await _context.Camps
                .Where(c => c.status == "PendingApproval" && c.createdAt.HasValue)
                .OrderByDescending(c => c.createdAt)
                .Select(c => new
                {
                    CampId = c.campId,
                    Name = c.name,
                    ManagerName = c.createByNavigation.firstName + " " + c.createByNavigation.lastName,
                    SubmittedDate = c.createdAt.Value,
                    Status = c.status
                })
                .ToListAsync();

            return pendingCamps.Select(x => (x.CampId, x.Name, x.ManagerName, x.SubmittedDate, x.Status)).ToList();
        }
    }
}
