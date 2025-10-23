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

        // Override GetAllAsync to include VehicleType navigation property
        public new async Task<List<Vehicle>> GetAllAsync()
        {
            Console.WriteLine("🔄 [VehicleRepository] GetAllAsync with Include(VehicleTypeNavigation)");
            var vehicles = await _context.Set<Vehicle>()
                .Include(v => v.vehicleTypeNavigation)
                .ToListAsync();

            Console.WriteLine($"✅ [VehicleRepository] Retrieved {vehicles.Count} vehicles");
            foreach (var vehicle in vehicles)
            {
                Console.WriteLine($"   - Vehicle {vehicle.vehicleId}: vehicleType={vehicle.vehicleType}, Navigation={vehicle.vehicleTypeNavigation?.name}");
            }

            return vehicles;
        }

        // Override GetByIdAsync to include VehicleType navigation property
        public new async Task<Vehicle?> GetByIdAsync(int id)
        {
            Console.WriteLine($"🔄 [VehicleRepository] GetByIdAsync({id}) with Include(VehicleTypeNavigation)");
            var vehicle = await _context.Set<Vehicle>()
                .Include(v => v.vehicleTypeNavigation)
                .FirstOrDefaultAsync(v => v.vehicleId == id);

            if (vehicle != null)
            {
                Console.WriteLine($"✅ [VehicleRepository] Found vehicle: vehicleType={vehicle.vehicleType}, Navigation={vehicle.vehicleTypeNavigation?.name}");
            }
            else
            {
                Console.WriteLine($"❌ [VehicleRepository] Vehicle with id {id} not found");
            }

            return vehicle;
        }
    }
}
