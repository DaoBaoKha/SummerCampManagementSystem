using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;

namespace SummerCampManagementSystem.DAL.Repositories.Repository
{
    public class RegistrationCancelRepository : GenericRepository<RegistrationCancel>, IRegistrationCancelRepository
    {
        public RegistrationCancelRepository(CampEaseDatabaseContext context) : base(context)
        {
        }
    }
}
