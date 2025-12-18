using SummerCampManagementSystem.DAL.Models;

namespace SummerCampManagementSystem.DAL.Repositories.Interfaces
{
    public interface ICampRepository : IGenericRepository<Camp>
    {
        Task<IEnumerable<Camp>> GetCampsByTypeAsync(int campTypeId);
        Task<IEnumerable<Camp>> GetCampsByStaffIdAsync(int staffId);

        // admin dashboard methods
        Task<int> GetActiveCampsCountAsync();
        Task<Dictionary<string, int>> GetCampStatusDistributionAsync();
        Task<List<(string Month, decimal Revenue)>> GetMonthlyRevenueAsync(int months);
        Task<List<(int CampId, string Name, string ManagerName, DateTime SubmittedDate, string Status)>> GetPendingCampsAsync();

        // camp report export methods
        Task<(string CampName, string CampType, string Location, DateTime? StartDate, DateTime? EndDate, string Status, int MaxParticipants, decimal? AverageRating, int TotalFeedbacks)> GetCampReportOverviewAsync(int campId);
        Task<(int TotalRequests, int PendingApproval, int Approved, int Confirmed, int Canceled, int Rejected)> GetRegistrationFunnelDataAsync(int campId);
        Task<List<(string CamperName, int Age, string Gender, string GuardianName, string GuardianPhone, string GroupName, string MedicalNotes, string TransportInfo, string AccommodationInfo)>> GetCamperRosterDataAsync(int campId);
        Task<List<(string TransactionCode, DateTime? TransactionDate, string PayerName, string Description, decimal Amount, string Status, string PaymentMethod)>> GetFinancialTransactionsAsync(int campId);
        Task<List<(string StaffName, string Role, string Email, string PhoneNumber, string AssignmentType)>> GetStaffAssignmentsAsync(int campId);
    }
}
