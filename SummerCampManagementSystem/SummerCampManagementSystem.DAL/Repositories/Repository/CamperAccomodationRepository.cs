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
    public class CamperAccomodationRepository : GenericRepository<CamperAccommodation>, ICamperAccomodationRepository
    {
        public CamperAccomodationRepository(CampEaseDatabaseContext context) : base(context)
        {
            _context = context;
        }

        public async Task<bool> IsAccommodationStaffOfCamper(int staffId, int camperId)
        {
            return await _context.CamperAccommodations
                .AnyAsync(ca => ca.camperId == camperId
                             && ca.accommodation.supervisorId == staffId);
        }

      
    }
}
