using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;

namespace SummerCampManagementSystem.DAL.Repositories.Repository
{
    public class CampStaffAssignmentRepository : GenericRepository<CampStaffAssignment>, ICampStaffAssignmentRepository
    {
        public CampStaffAssignmentRepository(CampEaseDatabaseContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<UserAccount>> GetAvailableStaffManagerByCampIdAsync(DateTime? start, DateTime? end)
        {
            var allStaff = await _context.UserAccounts
                    .Include(u => u.CampStaffAssignments)
                        .ThenInclude(csa => csa.camp)
                    .Where(u => u.role == "Staff" || u.role == "Manager")
                    .ToListAsync();

             var available = allStaff.Where(s =>
                    !s.CampStaffAssignments.Any(csa =>
                        csa.camp.startDate <= end &&
                        csa.camp.endDate >= start));

            return available;
        }
        public async Task<IEnumerable<UserAccount>> GetAvailableStaffByCampId(int campId)
        {
            return await _context.CampStaffAssignments
                .Where(csa => csa.campId == campId)
                .Select(csa => csa.staff)
                .Where(staff => staff.role == "Staff")
                .ToListAsync();
        }

        public async Task<bool> IsStaffBusyInAnyCampAsync(int staffId, DateOnly date)
        {
            var checkDate = date.ToDateTime(TimeOnly.MinValue);

            return await _context.CampStaffAssignments
                .Include(csa => csa.camp)
                .AnyAsync(csa =>
                    csa.staffId == staffId &&
                    csa.camp.startDate <= checkDate &&
                    csa.camp.endDate >= checkDate
                );
        }

        public async Task<IEnumerable<int>> GetStaffIdsByCampIdAsync(int campId)
        {
            var list = await _context.CampStaffAssignments
                .Where(x => x.campId == campId && x.staffId != null)
                .Select(x => x.staffId)
                .ToListAsync();

            return list.Where(id => id.HasValue).Select(id => id.Value);
        }

        // get available staff - bulk check
        public async Task<IEnumerable<int>> GetBusyStaffIdsInOtherActiveCampAsync(DateTime checkDate, int currentCampId)
        {
            var freeStatuses = new[] { "Completed", "Canceled", "Rejected", "Draft" }; // available status

            return await _context.CampStaffAssignments
                 .Include(csa => csa.camp)
                 .Where(csa =>
                     csa.staffId != null && 
                     csa.campId != currentCampId && // different from current camp
                     csa.camp.startDate <= checkDate &&
                     csa.camp.endDate >= checkDate &&
                     !freeStatuses.Contains(csa.camp.status)
                 )
                 .Select(csa => csa.staffId.Value)
                 .Distinct()
                 .ToListAsync();
        }

        // use for single validation
        public async Task<bool> IsStaffBusyInOtherActiveCampAsync(int staffId, DateTime checkDate, int currentCampId)
        {
            var freeStatuses = new[] { "Completed", "Canceled", "Rejected", "Draft" };

            return await _context.CampStaffAssignments
               .Include(csa => csa.camp)
               .AnyAsync(csa =>
                   csa.staffId == staffId &&
                   csa.campId != currentCampId &&
                   !freeStatuses.Contains(csa.camp.status) &&
                   csa.camp.startDate <= checkDate &&
                   csa.camp.endDate >= checkDate
               );
        }
    }
}
