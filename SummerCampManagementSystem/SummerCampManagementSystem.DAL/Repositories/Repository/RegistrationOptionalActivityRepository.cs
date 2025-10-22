using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;

namespace SummerCampManagementSystem.DAL.Repositories.Repository
{
    public class RegistrationOptionalActivityRepository : GenericRepository<RegistrationOptionalActivity>, IRegistrationOptionalActivityRepository
    {
        public RegistrationOptionalActivityRepository(CampEaseDatabaseContext context) : base(context)
        {
        }
    }
}
