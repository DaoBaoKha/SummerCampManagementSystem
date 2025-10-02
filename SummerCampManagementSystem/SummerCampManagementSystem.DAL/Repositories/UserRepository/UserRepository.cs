using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.GenericRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.DAL.Repositories.UserRepository
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        public UserRepository()
        {
        }

        public UserRepository(CampEaseDatabaseContext context)
        {
            _context = context;
        }
        public async Task<User?> GetUserAccount(string email, string password)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.email == email && u.password_hash == password);
        }
    }
}
