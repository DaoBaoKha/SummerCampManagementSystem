using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;

namespace SummerCampManagementSystem.DAL.Repositories.Repository
{
    public class RegistrationCancelRepository : GenericRepository<RegistrationCancel>, IRegistrationCancelRepository
    {
        public RegistrationCancelRepository(CampEaseDatabaseContext context) : base(context)
        {
        }

        public async Task<RegistrationCancel?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.RegistrationCancels
                .Include(rc => rc.registration)
                .FirstOrDefaultAsync(rc => rc.registrationCancelId == id);
        }

        public IQueryable<RegistrationCancel> GetQueryableWithDetails()
        {
            return _context.RegistrationCancels
                .Include(rc => rc.registration).ThenInclude(r => r.user) 
                .Include(rc => rc.registration)
                    .ThenInclude(r => r.RegistrationCampers)
                        .ThenInclude(rcp => rcp.camper) 
                .Include(rc => rc.bankUser); 
        }
    }
}
