using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;

namespace SummerCampManagementSystem.DAL.Repositories.Repository
{
    public class RegistrationRepository : GenericRepository<Registration>, IRegistrationRepository
    {
        public RegistrationRepository(CampEaseDatabaseContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Registration?> GetWithDetailsForRefundAsync(int registrationId)
        {
            return await _context.Registrations
                .Include(r => r.camp)
                .Include(r => r.Transactions)
                .Include(r => r.user)
                .FirstOrDefaultAsync(r => r.registrationId == registrationId);
        }
    }
}
