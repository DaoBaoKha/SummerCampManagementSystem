using SummerCampManagementSystem.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.DAL.Repositories.Interfaces
{
    public interface ICamperRepository : IGenericRepository<Camper>
    {
        new Task<IEnumerable<Camper>> GetAllAsync();
        new Task<Camper?> GetByIdAsync(int id);
        Task<Registration?> GetRegistrationByCamperIdAsync(int camperId);
        Task<IEnumerable<Camper>> GetGuardiansByCamperId(int camperId);
        Task<bool> IsStaffSupervisorOfCamperAsync(int staffId, int camperId);
        Task<IEnumerable<Camper>> GetCampersByOptionalActivityId(int optionalActivityId);
        Task<IEnumerable<Camper>> GetCampersByCoreScheduleAndStaffAsync(int activityScheduleId, int staffId);
        Task<IEnumerable<Camper>> GetCampersByCoreScheduleIdAsync(int activityScheduleId);
        Task<IEnumerable<Camper>> GetCampersByAccommodationScheduleAsync(int activityScheduleId);
        Task<IEnumerable<Camper>> GetCampersByAccommodationScheduleAndStaffAsync(int activityScheduleId, int staffId);
        Task<(Dictionary<string, int> Gender, Dictionary<int, int> AgeGroups)> GetCamperProfileByCampAsync(int campId);
    }
}
