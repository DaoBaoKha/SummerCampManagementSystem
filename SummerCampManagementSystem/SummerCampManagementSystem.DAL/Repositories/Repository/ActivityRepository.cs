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
    public class ActivityRepository : GenericRepository<Activity>, IActivityRepository
    {
        private readonly CampEaseDatabaseContext _context;
        public ActivityRepository(CampEaseDatabaseContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Activity>> GetByCampIdAsync(int id)
        {
            return await _context.Activities
                .Where(a => a.campId == id)
                .ToListAsync();
        }
    }
}
