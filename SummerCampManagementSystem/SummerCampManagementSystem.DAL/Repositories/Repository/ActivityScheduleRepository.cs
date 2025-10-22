using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;

namespace SummerCampManagementSystem.DAL.Repositories.Repository
{
    public class ActivityScheduleRepository : GenericRepository<ActivitySchedule>, IActivityScheduleRepository
    {
        public ActivityScheduleRepository(CampEaseDatabaseContext context) : base(context)
        {
        }
    }
}
