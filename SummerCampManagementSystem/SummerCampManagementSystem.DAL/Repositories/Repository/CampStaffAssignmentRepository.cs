using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;

namespace SummerCampManagementSystem.DAL.Repositories.Repository
{
    public class CampStaffAssignmentRepository : GenericRepository<CampStaffAssignment>, ICampStaffAssignmentRepository
    {
        public CampStaffAssignmentRepository(CampEaseDatabaseContext context) : base(context)
        {
        }
    }
}
