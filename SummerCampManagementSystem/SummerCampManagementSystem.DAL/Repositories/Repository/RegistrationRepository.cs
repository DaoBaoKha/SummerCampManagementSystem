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

        public async Task<decimal> GetTotalRevenueAsync(int campId)
        {
            // calculate total revenue from Confirmed transactions of Confirmed registrations
            var confirmedRevenue = await _context.Registrations
                .Where(r => r.campId == campId && r.status == RegistrationStatus.Confirmed.ToString())
                .SelectMany(r => r.Transactions)
                .Where(t => t.status == TransactionStatus.Confirmed.ToString())
                .SumAsync(t => t.amount ?? 0);

            // subtract refunded amounts
            var refundedAmount = await _context.Registrations
                .Where(r => r.campId == campId && r.status == RegistrationStatus.Refunded.ToString())
                .SelectMany(r => r.Transactions)
                .Where(t => t.status == TransactionStatus.Refunded.ToString())
                .SumAsync(t => t.amount ?? 0);

            return confirmedRevenue - refundedAmount;
        }

        public async Task<int> GetPendingApprovalsCountAsync(int campId)
        {
            return await _context.Registrations
                .Where(r => r.campId == campId && r.status == RegistrationStatus.PendingApproval.ToString())
                .CountAsync();
        }

        public async Task<double> GetCancellationRateAsync(int campId)
        {
            var totalRegistrations = await _context.Registrations
                .Where(r => r.campId == campId)
                .CountAsync();

            if (totalRegistrations == 0)
                return 0;

            var canceledRegistrations = await _context.Registrations
                .Where(r => r.campId == campId && r.status == RegistrationStatus.Canceled.ToString())
                .CountAsync();

            return (double)canceledRegistrations / totalRegistrations * 100;
        }

        public async Task<List<(DateTime Date, int Count, decimal Revenue)>> GetRegistrationTrendAsync(int campId)
        {
            var registrations = await _context.Registrations
                .Where(r => r.campId == campId && r.registrationCreateAt.HasValue)
                .Include(r => r.Transactions)
                .ToListAsync();

            var trendData = registrations
                .GroupBy(r => r.registrationCreateAt.Value.Date)
                .Select(g => (
                    Date: g.Key,
                    Count: g.Count(),
                    Revenue: g.SelectMany(r => r.Transactions)
                             .Where(t => t.status == TransactionStatus.Confirmed.ToString())
                             .Sum(t => t.amount ?? 0)
                ))
                .OrderBy(x => x.Date)
                .ToList();

            return trendData;
        }

        public async Task<Dictionary<string, int>> GetStatusDistributionAsync(int campId)
        {
            var statusCounts = await _context.Registrations
                .Where(r => r.campId == campId)
                .GroupBy(r => r.status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            return statusCounts.ToDictionary(x => x.Status ?? "Unknown", x => x.Count);
        }

        public async Task<List<(int RegistrationId, string CamperName, DateTime RegistrationDate, string Status, decimal Amount, string Avatar)>> GetRecentRegistrationsAsync(int campId, int limit)
        {
            var recentRegistrations = await _context.Registrations
                .Where(r => r.campId == campId)
                .OrderByDescending(r => r.registrationCreateAt)
                .Take(limit)
                .Include(r => r.RegistrationCampers)
                    .ThenInclude(rc => rc.camper)
                .Include(r => r.Transactions)
                .ToListAsync();

            var result = recentRegistrations.Select(r =>
            {
                var firstCamper = r.RegistrationCampers.FirstOrDefault()?.camper;
                var totalAmount = r.Transactions
                    .Where(t => t.status == TransactionStatus.Confirmed.ToString())
                    .Sum(t => t.amount ?? 0);

                return (
                    RegistrationId: r.registrationId,
                    CamperName: firstCamper?.camperName ?? "N/A",
                    RegistrationDate: r.registrationCreateAt ?? DateTime.MinValue,
                    Status: r.status ?? "Unknown",
                    Amount: totalAmount,
                    Avatar: firstCamper?.avatar ?? ""
                );
            }).ToList();

            return result;
        }
    }
}
