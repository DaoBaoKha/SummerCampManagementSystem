using SummerCampManagementSystem.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.DAL.Repositories.Interfaces
{
    public interface IFeedbackRepository : IGenericRepository<Feedback>
    {
        Task<IEnumerable<Feedback>> GetFeedbacksByCampIdAsync(int campId);
        Task<IEnumerable<Feedback>> GetFeedbacksByRegistrationIdAsync(int registrationId);
    }
}
