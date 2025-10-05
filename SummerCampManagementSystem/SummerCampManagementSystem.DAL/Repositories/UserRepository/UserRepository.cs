using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.GenericRepository;

namespace SummerCampManagementSystem.DAL.Repositories.UserRepository
{
    public class UserRepository : GenericRepository<UserAccount>, IUserRepository
    {
        public UserRepository()
        {
        }

        public UserRepository(CampEaseDatabaseContext context)
        {
            _context = context;
        }
        public async Task<UserAccount?> GetUserAccount(string email, string password)
        {
            return await _context.UserAccounts
                .FirstOrDefaultAsync(u => u.email == email && u.password == password);
        }
    }
}
