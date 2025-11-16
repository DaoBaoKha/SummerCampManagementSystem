using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;

namespace SummerCampManagementSystem.DAL.Repositories.Repository
{
    public class TransportScheduleRepository : GenericRepository<TransportSchedule>, ITransportScheduleRepository
    {
        public TransportScheduleRepository(CampEaseDatabaseContext context) : base(context)
        {
        }
    }
}
