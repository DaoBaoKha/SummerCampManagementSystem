using SummerCampManagementSystem.DAL.Models;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface IUserService
    {
        Task<UserAccount?> Login(string email, string password);
    }
}
