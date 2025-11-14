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
        public async Task<IEnumerable<UserAccount>> GetAllStaffWithCampAssignmentsAsync()
        {
            return await _context.UserAccounts
                .Include(u => u.CampStaffAssignments)
                    .ThenInclude(csa => csa.camp)
                .Where(u => u.role == "Staff")
                .ToListAsync();
        }
   
    }
}
