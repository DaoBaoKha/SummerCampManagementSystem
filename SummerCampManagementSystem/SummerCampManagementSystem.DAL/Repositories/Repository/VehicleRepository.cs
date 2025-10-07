using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;

namespace SummerCampManagementSystem.DAL.Repositories.Repository
{
    public class VehicleRepository : GenericRepository<Vehicle>, IVehicleRepository
    {
     
        public VehicleRepository(CampEaseDatabaseContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Vehicle?> GetVehicleById(int id)
        {
            var vehicle = await GetByIdAsync(id);
            return vehicle;
        }
    }
}
