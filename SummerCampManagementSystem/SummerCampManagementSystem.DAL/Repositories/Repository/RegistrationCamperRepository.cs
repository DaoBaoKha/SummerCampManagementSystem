using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.DAL.Repositories.Repository
{
    public class RegistrationCamperRepository : GenericRepository<RegistrationCamper>, IRegistrationCamperRepository
    {
        public RegistrationCamperRepository(CampEaseDatabaseContext context) : base(context)
        {
            _context = context;
        }
        public async Task<RegistrationCamper?> GetByCamperId(int camperId)
        {
            return await _context.RegistrationCampers.FirstOrDefaultAsync
                (rc => rc.camperId == camperId);
        }
    }
}
