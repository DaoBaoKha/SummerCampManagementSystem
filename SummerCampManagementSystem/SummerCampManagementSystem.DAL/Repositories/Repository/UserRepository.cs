using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;

namespace SummerCampManagementSystem.DAL.Repositories.Repository
{
    public class UserRepository : GenericRepository<UserAccount>, IUserRepository
    {

        public UserRepository(CampEaseDatabaseContext context) : base(context)
        {
            _context = context;
        }

        public async Task<UserAccount?> GetUserByEmail(string email)
        {
            return await _context.UserAccounts
                .FirstOrDefaultAsync(u => u.email == email);
        }
    }
}
