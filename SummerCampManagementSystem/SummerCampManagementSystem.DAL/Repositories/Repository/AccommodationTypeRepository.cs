using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;

namespace SummerCampManagementSystem.DAL.Repositories.Repository
{
    public class AccommodationTypeRepository : GenericRepository<AccommodationType>, IAccommodationTypeRepository
    {
        public AccommodationTypeRepository(CampEaseDatabaseContext context) : base(context)
        {
        }
    }
}
