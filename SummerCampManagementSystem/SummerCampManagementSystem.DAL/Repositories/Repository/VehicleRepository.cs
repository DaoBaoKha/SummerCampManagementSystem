using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.DAL.Repositories.Repository
{
    public class VehicleRepository : GenericRepository<Vehicle>, IVehicleRepository
    {
     
        public VehicleRepository(CampEaseDatabaseContext context) : base(context)
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
