using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;

namespace SummerCampManagementSystem.DAL.Repositories.Repository
{
    public class BankUserRepository : GenericRepository<BankUser>, IBankUserRepository
    {
        public BankUserRepository(CampEaseDatabaseContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<BankUser>> GetByUserIdAsync(int userId)
        {
            return await _context.BankUsers
                .Where(b => b.userId == userId && b.isActive == true)
                .OrderByDescending(b => b.isPrimary) // get primary bank account first
                .ToListAsync();
        }

        public async Task<BankUser?> GetPrimaryByUserIdAsync(int userId)
        {
            return await _context.BankUsers
                .FirstOrDefaultAsync(b => b.userId == userId && b.isActive == true && b.isPrimary == true);
        }
    }
}
