using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.Core.Enums;
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

        public async Task<Registration?> GetDetailsForStatusUpdateAsync(int registrationId)
        {
            // include campers for potential business logic checks
            return await _context.Registrations
                .Include(r => r.RegistrationCampers)
                .FirstOrDefaultAsync(r => r.registrationId == registrationId);
        }

        public async Task<Registration?> GetWithCampersAsync(int id)
        {
            return await _context.Registrations
                .Include(r => r.RegistrationCampers)
                .FirstOrDefaultAsync(r => r.registrationId == id);
        }

        public async Task<Registration?> GetForUpdateAsync(int id)
        {
            return await _context.Registrations
                .Include(r => r.RegistrationCampers)
                .AsNoTracking() // no tracking for update scenarios
                .FirstOrDefaultAsync(r => r.registrationId == id);
        }

        public async Task<Registration?> GetFullDetailsAsync(int id)
        {
            return await _context.Registrations
                .Where(r => r.registrationId == id)
                .Include(r => r.camp)
                .Include(r => r.user) 
                .Include(r => r.RegistrationCampers).ThenInclude(rc => rc.camper)
                .Include(r => r.appliedPromotion)
                .Include(r => r.RegistrationOptionalActivities)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Registration>> GetAllWithDetailsAsync()
        {
            return await _context.Registrations
                .Include(r => r.camp)
                .Include(r => r.user)
                .Include(r => r.RegistrationCampers).ThenInclude(rc => rc.camper)
                .Include(r => r.appliedPromotion)
                .Include(r => r.RegistrationOptionalActivities)
                .ToListAsync();
        }

        public async Task<IEnumerable<Registration>> GetByStatusAsync(string status)
        {
            return await _context.Registrations
                .Where(r => r.status == status)
                .Include(r => r.camp)
                .Include(r => r.user)
                .Include(r => r.RegistrationCampers).ThenInclude(rc => rc.camper)
                .Include(r => r.appliedPromotion)
                .ToListAsync(); 
        }

        public async Task<Registration?> GetForPaymentAsync(int id)
        {
            return await _context.Registrations
                .Include(r => r.camp)
                .Include(r => r.user)
                .Include(r => r.RegistrationCampers).ThenInclude(rc => rc.camper)
                .Include(r => r.appliedPromotion)
                .Include(r => r.RegistrationOptionalActivities)
                    .ThenInclude(roa => roa.activitySchedule)
                .FirstOrDefaultAsync(r => r.registrationId == id);
        }

        public async Task<bool> IsCamperRegisteredAsync(int campId, int camperId)
        {
            return await _context.Registrations
                .Where(r => r.campId == campId &&
                            r.RegistrationCampers.Any(rc => rc.camperId == camperId) &&
                            (r.status == RegistrationStatus.Approved.ToString() ||
                             r.status == RegistrationStatus.PendingApproval.ToString() ||
                             r.status == RegistrationStatus.PendingPayment.ToString()))
                .AnyAsync();
        }

        public async Task<IEnumerable<Registration>> GetHistoryByUserIdAsync(int userId)
        {
            return await _context.Registrations
                .Where(r => r.userId == userId)
                .OrderByDescending(r => r.registrationCreateAt)
                .Include(r => r.camp)
                .Include(r => r.RegistrationCampers).ThenInclude(rc => rc.camper)
                .Include(r => r.appliedPromotion)
                .Include(r => r.RegistrationOptionalActivities)
                .ToListAsync();
        }

        public async Task<IEnumerable<Registration>> GetByCampIdAsync(int campId)
        {
            return await _context.Registrations
                .Where(r => r.campId == campId)
                .Include(r => r.camp)
                .Include(r => r.RegistrationCampers).ThenInclude(rc => rc.camper)
                .Include(r => r.appliedPromotion)
                .Include(r => r.RegistrationOptionalActivities)
                .ToListAsync();
        }
    }
}
