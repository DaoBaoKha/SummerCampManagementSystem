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

        public async Task<IEnumerable<UserAccount>> GetAvailableStaffByCampIdAsync(DateTime? start, DateTime? end)
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
        public async Task<IEnumerable<UserAccount>> GetAvailableStaffByCampForActivityAsync(int campId)
        {
            var camp = await _context.Camps
                .Where(c => c.campId == campId)
                .Select(c => new { c.startDate, c.endDate })
                .FirstOrDefaultAsync();

            if (camp == null)
                throw new KeyNotFoundException("Camp not found.");

            var (start, end) = (camp.startDate, camp.endDate);

            return await _context.UserAccounts
                .Where(u => u.role == "Staff")
                .Where(u =>
                    !u.CampStaffAssignments.Any(csa =>
                        csa.campId != campId &&
                        csa.camp.startDate <= end &&
                        csa.camp.endDate >= start))
                .ToListAsync();
        }

    }
}
