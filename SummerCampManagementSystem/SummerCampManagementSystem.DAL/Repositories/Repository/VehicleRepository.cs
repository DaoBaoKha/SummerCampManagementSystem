using Microsoft.EntityFrameworkCore;
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

        public async Task<IEnumerable<Vehicle>> GetAvailableVehicles()
        {
            return await _context.Vehicles
                .Where(v => v.status.ToLower() == "active")
                .ToListAsync();
        }
    }
}
