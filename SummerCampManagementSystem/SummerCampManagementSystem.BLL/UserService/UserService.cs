using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.UserRepository;
using SummerCampManagementSystem.DAL.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.UserService
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        public UserService() => _userRepository ??= new UserRepository();
        public async Task<User?> Login(string email, string password)
        {
            return await _userRepository.GetUserAccount(email, password);
        }
    }
}
