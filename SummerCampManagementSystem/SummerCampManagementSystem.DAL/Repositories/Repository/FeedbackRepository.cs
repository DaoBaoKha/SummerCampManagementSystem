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
    public class FeedbackRepository : GenericRepository<Feedback>, IFeedbackRepository
    {
        public FeedbackRepository(CampEaseDatabaseContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Feedback>> GetFeedbacksByCampIdAsync(int campId)
        {
            return await _context.Feedbacks
                .Include(f => f.registration)
                    .ThenInclude(r => r.camp)
                .Where(f => f.registration.campId == campId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Feedback>> GetFeedbacksByRegistrationIdAsync(int registrationId)
        {
            return await _context.Feedbacks
                .Include(f => f.registration)
                .Where(f => f.registrationId == registrationId)
                .ToListAsync();
        }
    }
}
