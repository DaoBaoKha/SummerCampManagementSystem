using SummerCampManagementSystem.DAL.Models;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface IUserService
    {
        Task<User?> Login(string email, string password);
    }
}
