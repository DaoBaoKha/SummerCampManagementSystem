using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.Core.Enums;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;

namespace SummerCampManagementSystem.DAL.Repositories.Repository
{
    public class CampRepository : GenericRepository<Camp>, ICampRepository
    {
        public CampRepository(CampEaseDatabaseContext context) : base(context)
        {
        }
        public async Task<IEnumerable<Camp>> GetCampsByTypeAsync(int campTypeId)
        {
            return await _context.Camps.Where(c => c.campTypeId == campTypeId).ToListAsync();
        }

        public async Task<IEnumerable<Camp>> GetCampsByStaffIdAsync(int staffId)
        {
            return await _context.Camps
                .Where(c =>
                    c.status != CampStatus.Draft.ToString()
                    && c.status != CampStatus.PendingApproval.ToString()
                    &&
                    (
                        c.Activities.Any(a =>
                            a.ActivitySchedules.Any(s => s.staffId == staffId)
                        )
                        || c.Groups.Any(g => g.supervisorId == staffId)
                        || c.Accommodations.Any(a => a.supervisorId == staffId)
                    )
                )
                .Distinct()
                .ToListAsync();
        }

        // ADMIN DASHBOARD METHODS
        public async Task<int> GetActiveCampsCountAsync()
        {
            // active camps = all camps except Draft, PendingApproval, Rejected, UnderEnrolled, Canceled
            var inactiveStatuses = new[] { "Draft", "PendingApproval", "Rejected", "UnderEnrolled", "Canceled" };
            
            return await _context.Camps
                .Where(c => !inactiveStatuses.Contains(c.status))
                .CountAsync();
        }

        public async Task<Dictionary<string, int>> GetCampStatusDistributionAsync()
        {
            var distribution = await _context.Camps
                .GroupBy(c => c.status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            return distribution.ToDictionary(x => x.Status, x => x.Count);
        }

        public async Task<List<(string Month, decimal Revenue)>> GetMonthlyRevenueAsync(int months)
        {
            var startDate = DateTime.UtcNow.Date.AddMonths(-months);

            var monthlyRevenue = await _context.Transactions
                .Where(t => t.transactionTime.HasValue && 
                           t.transactionTime >= startDate &&
                           (t.status == "Confirmed" || t.status == "Refunded"))
                .GroupBy(t => new { Year = t.transactionTime.Value.Year, Month = t.transactionTime.Value.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Revenue = g.Where(t => t.status == "Confirmed").Sum(t => t.amount ?? 0) -
                             g.Where(t => t.status == "Refunded").Sum(t => t.amount ?? 0)
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToListAsync();

            return monthlyRevenue.Select(x => ($"{x.Month:D2}/{x.Year}", x.Revenue)).ToList();
        }

        public async Task<List<(int CampId, string Name, string ManagerName, DateTime SubmittedDate, string Status)>> GetPendingCampsAsync()
        {
            var pendingCamps = await _context.Camps
                .Where(c => c.status == "PendingApproval" && c.createdAt.HasValue)
                .OrderByDescending(c => c.createdAt)
                .Select(c => new
                {
                    CampId = c.campId,
                    Name = c.name,
                    ManagerName = c.createByNavigation.firstName + " " + c.createByNavigation.lastName,
                    SubmittedDate = c.createdAt.Value,
                    Status = c.status
                })
                .ToListAsync();

            return pendingCamps.Select(x => (x.CampId, x.Name, x.ManagerName, x.SubmittedDate, x.Status)).ToList();
        }

        // CAMP REPORT EXPORT METHODS
        public async Task<(string CampName, string CampType, string Location, DateTime? StartDate, DateTime? EndDate, string Status, int MaxParticipants, decimal? AverageRating, int TotalFeedbacks)> GetCampReportOverviewAsync(int campId)
        {
            var camp = await _context.Camps
                .Include(c => c.campType)
                .Include(c => c.location)
                .FirstOrDefaultAsync(c => c.campId == campId);

            if (camp == null)
                return (null, null, null, null, null, null, 0, null, 0);

            var campName = camp.name ?? "N/A";
            var campType = camp.campType?.name ?? "N/A";
            var location = camp.location?.name ?? "N/A";
            var startDate = camp.startDate;
            var endDate = camp.endDate;
            var status = camp.status ?? "N/A";
            var maxParticipants = camp.maxParticipants ?? 0;

            // calculate average rating from feedbacks (via registrations)
            var feedbacks = await _context.Feedbacks
                .Where(f => f.registration.campId == campId && f.rating.HasValue)
                .ToListAsync();
            var averageRating = feedbacks.Any() ? (decimal?)feedbacks.Average(f => f.rating.Value) : null;
            var totalFeedbacks = await _context.Feedbacks.CountAsync(f => f.registration.campId == campId);

            return (campName, campType, location, startDate, endDate, status, maxParticipants, averageRating, totalFeedbacks);
        }

        public async Task<(int TotalRequests, int PendingApproval, int Approved, int Confirmed, int Canceled, int Rejected)> GetRegistrationFunnelDataAsync(int campId)
        {
            var registrations = await _context.Registrations
                .Where(r => r.campId == campId)
                .ToListAsync();

            var totalRequests = registrations.Count;
            var pendingApproval = registrations.Count(r => r.status == RegistrationStatus.PendingApproval.ToString());
            var approved = registrations.Count(r => r.status == RegistrationStatus.Approved.ToString());
            var confirmed = registrations.Count(r => r.status == RegistrationStatus.Confirmed.ToString());
            var canceled = registrations.Count(r => r.status == RegistrationStatus.Canceled.ToString());
            var rejected = registrations.Count(r => r.status == RegistrationStatus.Rejected.ToString());

            return (totalRequests, pendingApproval, approved, confirmed, canceled, rejected);
        }

        public async Task<List<(string CamperName, int Age, string Gender, string GuardianName, string GuardianPhone, string GroupName, string MedicalNotes, string TransportInfo, string AccommodationInfo)>> GetCamperRosterDataAsync(int campId)
        {
            var camperData = await _context.RegistrationCampers
                .Where(rc => rc.registration.campId == campId && 
                            (rc.status == RegistrationCamperStatus.Confirmed.ToString() || 
                             rc.status == RegistrationCamperStatus.CheckedIn.ToString() || 
                             rc.status == RegistrationCamperStatus.Approved.ToString()))
                .Include(rc => rc.camper)
                    .ThenInclude(c => c.CamperGuardians)
                    .ThenInclude(cg => cg.guardian)
                .Include(rc => rc.camper)
                    .ThenInclude(c => c.CamperGroups)
                    .ThenInclude(cg => cg.group)
                .Include(rc => rc.camper)
                    .ThenInclude(c => c.CamperTransports)
                    .ThenInclude(ct => ct.transportSchedule)
                .Include(rc => rc.camper)
                    .ThenInclude(c => c.CamperAccommodations)
                    .ThenInclude(ca => ca.accommodation)
                .Include(rc => rc.camper)
                    .ThenInclude(c => c.HealthRecord)
                .Select(rc => new
                {
                    CamperName = rc.camper.camperName ?? "N/A",
                    Age = rc.camper.dob.HasValue ? DateTime.Now.Year - rc.camper.dob.Value.Year : 0,
                    Gender = rc.camper.gender ?? "N/A",
                    GuardianName = rc.camper.CamperGuardians.FirstOrDefault() != null 
                        ? rc.camper.CamperGuardians.FirstOrDefault().guardian.fullName ?? "N/A"
                        : "N/A",
                    GuardianPhone = rc.camper.CamperGuardians.FirstOrDefault() != null
                        ? rc.camper.CamperGuardians.FirstOrDefault().guardian.phoneNumber ?? "N/A"
                        : "N/A",
                    GroupName = rc.camper.CamperGroups.FirstOrDefault() != null
                        ? rc.camper.CamperGroups.FirstOrDefault().group.groupName ?? "N/A"
                        : "Chưa phân nhóm",
                    MedicalNotes = rc.camper.HealthRecord != null 
                        ? (rc.camper.HealthRecord.allergies ?? "Không có") + (string.IsNullOrEmpty(rc.camper.HealthRecord.note) ? "" : " - " + rc.camper.HealthRecord.note)
                        : "Không có",
                    TransportInfo = rc.camper.CamperTransports.Any()
                        ? rc.camper.CamperTransports.FirstOrDefault().transportSchedule.transportType ?? "Có đăng ký"
                        : "Tự túc",
                    AccommodationInfo = rc.camper.CamperAccommodations.FirstOrDefault() != null
                        ? rc.camper.CamperAccommodations.FirstOrDefault().accommodation.name ?? "N/A"
                        : "Chưa phân phòng"
                })
                .ToListAsync();

            return camperData.Select(x => (x.CamperName, x.Age, x.Gender, x.GuardianName, x.GuardianPhone, x.GroupName, x.MedicalNotes, x.TransportInfo, x.AccommodationInfo)).ToList();
        }

        public async Task<List<(string TransactionCode, DateTime? TransactionDate, string PayerName, string Description, decimal Amount, string Status, string PaymentMethod)>> GetFinancialTransactionsAsync(int campId)
        {
            var transactions = await _context.Transactions
                .Where(t => t.registration.campId == campId)
                .Include(t => t.registration)
                    .ThenInclude(r => r.user)
                .OrderByDescending(t => t.transactionTime)
                .Select(t => new
                {
                    TransactionCode = t.transactionCode ?? "N/A",
                    TransactionDate = t.transactionTime,
                    PayerName = t.registration.user != null 
                        ? (t.registration.user.firstName + " " + t.registration.user.lastName)
                        : "N/A",
                    Description = t.type == "Payment" 
                        ? $"Thanh toán đơn đăng ký #{t.registrationId}"
                        : $"Hoàn tiền đơn #{t.registrationId}",
                    Amount = t.type == "Refund" ? -(t.amount ?? 0) : (t.amount ?? 0),
                    Status = t.status ?? "N/A",
                    PaymentMethod = t.method ?? "N/A"
                })
                .ToListAsync();

            return transactions.Select(x => (x.TransactionCode, x.TransactionDate, x.PayerName, x.Description, x.Amount, x.Status, x.PaymentMethod)).ToList();
        }

        public async Task<List<(string StaffName, string Role, string Email, string PhoneNumber, string AssignmentType)>> GetStaffAssignmentsAsync(int campId)
        {
            // get staff from CampStaffAssignment
            var campStaff = await _context.CampStaffAssignments
                .Where(csa => csa.campId == campId)
                .Include(csa => csa.staff)
                .Select(csa => new
                {
                    StaffName = csa.staff.firstName + " " + csa.staff.lastName,
                    Role = csa.staff.role ?? "N/A",
                    Email = csa.staff.email ?? "N/A",
                    PhoneNumber = csa.staff.phoneNumber ?? "N/A",
                    AssignmentType = "Nhân viên trại"
                })
                .ToListAsync();

            // get staff from Groups (supervisors)
            var groupSupervisors = await _context.Groups
                .Where(g => g.campId == campId && g.supervisorId.HasValue)
                .Include(g => g.supervisor)
                .Select(g => new
                {
                    StaffName = g.supervisor.firstName + " " + g.supervisor.lastName,
                    Role = g.supervisor.role ?? "N/A",
                    Email = g.supervisor.email ?? "N/A",
                    PhoneNumber = g.supervisor.phoneNumber ?? "N/A",
                    AssignmentType = $"Giám sát nhóm: {g.groupName}"
                })
                .ToListAsync();

            // get staff from ActivitySchedules
            var activityStaff = await _context.ActivitySchedules
                .Where(asc => asc.activity.campId == campId && asc.staffId.HasValue)
                .Include(asc => asc.staff)
                .Include(asc => asc.activity)
                .Select(asc => new
                {
                    StaffName = asc.staff.firstName + " " + asc.staff.lastName,
                    Role = asc.staff.role ?? "N/A",
                    Email = asc.staff.email ?? "N/A",
                    PhoneNumber = asc.staff.phoneNumber ?? "N/A",
                    AssignmentType = $"Hoạt động: {asc.activity.name}"
                })
                .ToListAsync();

            // combine all staff
            var allStaff = campStaff
                .Concat(groupSupervisors)
                .Concat(activityStaff)
                .Distinct()
                .ToList();

            return allStaff.Select(x => (x.StaffName, x.Role, x.Email, x.PhoneNumber, x.AssignmentType)).ToList();
        }
    }
}
