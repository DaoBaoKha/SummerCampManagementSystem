using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.GenericRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.DAL.Repositories.VehicleRepository
{
    public class VehicleRepository : GenericRepository<Vehicle>, IVehicleRepository
    {
        public VehicleRepository()
        {
        }

        public VehicleRepository(CampEaseDatabaseContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Vehicle>> GetAllVehicles()
        {
            var vehicles = await GetAllAsync();
            return vehicles;
        }

        public async Task<Vehicle?> GetVehicleById(int id)
        {
            var vehicle = await GetByIdAsync(id);
            return vehicle;
        }
    }
}
