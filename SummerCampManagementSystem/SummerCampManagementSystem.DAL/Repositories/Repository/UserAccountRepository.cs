using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;

namespace SummerCampManagementSystem.DAL.Repositories.Repository
{
    public class UserAccountRepository : GenericRepository<UserAccount>, IUserAccountRepository
    {
        public UserAccountRepository(CampEaseDatabaseContext context) : base(context)
        {
        }

        // ADMIN DASHBOARD METHODS
        public async Task<int> GetTotalCustomersAsync()
        {
            return await _context.UserAccounts
                .Where(u => u.role == "User")
                .CountAsync();
        }

        public async Task<Dictionary<string, int>> GetWorkforceDistributionAsync()
        {
            var workforceRoles = new[] { "Manager", "Staff", "Driver" };
            
            var distribution = await _context.UserAccounts
                .Where(u => workforceRoles.Contains(u.role))
                .GroupBy(u => u.role)
                .Select(g => new { Role = g.Key, Count = g.Count() })
                .ToListAsync();

            return distribution.ToDictionary(x => x.Role, x => x.Count);
        }

        public async Task<List<(DateTime Date, int Count)>> GetNewCustomerGrowthAsync(int days)
        {
            var startDate = DateTime.UtcNow.Date.AddDays(-days);
            
            var newCustomers = await _context.UserAccounts
                .Where(u => u.role == "User" && u.createAt >= startDate)
                .GroupBy(u => u.createAt.Value.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .OrderBy(x => x.Date)
                .ToListAsync();

            return newCustomers.Select(x => (x.Date, x.Count)).ToList();
        }

        public async Task<List<(int UserId, string FullName, string Email, string Role, DateTime RegisteredDate)>> GetRecentUsersAsync(int limit)
        {
            var recentUsers = await _context.UserAccounts
                .Where(u => u.role == "User" && u.createAt.HasValue)
                .OrderByDescending(u => u.createAt)
                .Take(limit)
                .Select(u => new
                {
                    UserId = u.userId,
                    FullName = u.firstName + " " + u.lastName,
                    Email = u.email,
                    Role = u.role,
                    RegisteredDate = u.createAt.Value
                })
                .ToListAsync();

            return recentUsers.Select(x => (x.UserId, x.FullName, x.Email, x.Role, x.RegisteredDate)).ToList();
        }
    }
}
