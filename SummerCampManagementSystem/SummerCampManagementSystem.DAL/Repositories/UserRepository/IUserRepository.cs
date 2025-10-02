using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.GenericRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.DAL.Repositories.UserRepository
{
    public interface IUserRepository : IGenericRepository<User>
    {
        Task<User?> GetUserAccount(string email, string password);
    }
}
