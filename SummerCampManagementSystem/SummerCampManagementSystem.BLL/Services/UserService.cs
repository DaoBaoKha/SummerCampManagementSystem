using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.UserRepository;

namespace SummerCampManagementSystem.BLL.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        public UserService() => _userRepository ??= new UserRepository();
        public async Task<UserAccount?> Login(string email, string password)
        {
            return await _userRepository.GetUserAccount(email, password);
        }
    }
}
