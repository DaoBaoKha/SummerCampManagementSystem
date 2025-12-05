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

        public async Task<IEnumerable<Vehicle>> GetAllVehiclesWithType()
        {
            return await _context.Vehicles
                .Include(v => v.vehicleTypeNavigation)
                .ToListAsync();
        }

        public async Task<Vehicle?> GetVehicleWithTypeById(int id)
        {
            return await _context.Vehicles
                .Include(v => v.vehicleTypeNavigation)
                .FirstOrDefaultAsync(v => v.vehicleId == id);
        }

        public async Task<IEnumerable<Vehicle>> GetAvailableVehicles()
        {
            return await _context.Vehicles
                .Include(v => v.vehicleTypeNavigation)
                .Where(v => v.status.ToLower() == "active")
                .ToListAsync();
        }
    }
}
