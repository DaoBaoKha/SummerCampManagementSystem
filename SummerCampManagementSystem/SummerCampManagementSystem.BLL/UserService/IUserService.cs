using SummerCampManagementSystem.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.UserService
{
    public interface IUserService
    {
        Task<User?> Login(string email, string password);
    }
}
