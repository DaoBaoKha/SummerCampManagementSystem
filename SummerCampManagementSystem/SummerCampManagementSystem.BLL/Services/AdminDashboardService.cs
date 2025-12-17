using SummerCampManagementSystem.BLL.DTOs.Dashboard;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;

namespace SummerCampManagementSystem.BLL.Services
{
    public class AdminDashboardService : IAdminDashboardService
    {
        private readonly IRegistrationRepository _registrationRepository;
        private readonly ICampRepository _campRepository;
        private readonly IUserAccountRepository _userAccountRepository;
        private readonly ILocationRepository _locationRepository;

        public AdminDashboardService(IRegistrationRepository registrationRepository, ICampRepository campRepository, IUserAccountRepository userAccountRepository, ILocationRepository locationRepository)
        {
            _registrationRepository = registrationRepository;
            _campRepository = campRepository;
            _userAccountRepository = userAccountRepository;
            _locationRepository = locationRepository;
        }

        public async Task<AdminDashboardSummaryDto> GetDashboardSummaryAsync()
        {
            // get total system revenue
            var totalRevenue = await _registrationRepository.GetTotalSystemRevenueAsync();
            var previousMonthRevenue = await _registrationRepository.GetPreviousMonthRevenueAsync();

            // calculate revenue growth
            double? revenueGrowth = null;
            if (previousMonthRevenue > 0)
            {
                revenueGrowth = (double)((totalRevenue - previousMonthRevenue) / previousMonthRevenue * 100);
            }

            // get total customers (role = "User")
            var totalCustomers = await _userAccountRepository.GetTotalCustomersAsync();

            // get workforce distribution
            var workforceDistribution = await _userAccountRepository.GetWorkforceDistributionAsync();
            var totalWorkforce = workforceDistribution.Values.Sum();

            // get active camps count
            var activeCamps = await _campRepository.GetActiveCampsCountAsync();

            return new AdminDashboardSummaryDto
            {
                TotalRevenue = new KpiMetricDto
                {
                    Value = totalRevenue,
                    Growth = revenueGrowth.HasValue ? Math.Round(revenueGrowth.Value, 2) : null,
                    Label = "so với tháng trước"
                },
                TotalCustomers = new KpiMetricDto
                {
                    Value = totalCustomers,
                    Growth = null,
                    Label = "Tổng số người dùng với vai trò 'User'"
                },
                TotalWorkforce = new KpiMetricDto
                {
                    Value = totalWorkforce,
                    Growth = null,
                    Label = "Tổng số Quản lý + Nhân viên + Tài xế"
                },
                TotalActiveCamps = activeCamps
            };
        }

        public async Task<AdminUserAnalyticsDto> GetUserAnalyticsAsync()
        {
            // get workforce distribution
            var workforceDistribution = await _userAccountRepository.GetWorkforceDistributionAsync();

            // get new customer growth for last 30 days
            var customerGrowthData = await _userAccountRepository.GetNewCustomerGrowthAsync(30);
            var newCustomerGrowth = customerGrowthData.Select(x => new DailyGrowthDto
            {
                Date = x.Date.ToString("yyyy-MM-dd"),
                Count = x.Count
            }).ToList();

            return new AdminUserAnalyticsDto
            {
                WorkforceDistribution = workforceDistribution,
                NewCustomerGrowth = newCustomerGrowth
            };
        }

        public async Task<AdminLocationAnalyticsDto> GetLocationAnalyticsAsync()
        {
            // get top 5 locations by camp count
            var topLocations = await _locationRepository.GetTopLocationsByCampCountAsync(5);
            var locationStats = topLocations.Select(l => new LocationStatsDto
            {
                LocationId = l.LocationId,
                Name = l.Name,
                CampCount = l.CampCount,
                ActiveCamps = l.ActiveCamps
            }).ToList();

            return new AdminLocationAnalyticsDto
            {
                TopLocationsByCampCount = locationStats
            };
        }

        public async Task<AdminCampAnalyticsDto> GetCampAnalyticsAsync()
        {
            // get camp status distribution
            var statusOverview = await _campRepository.GetCampStatusDistributionAsync();

            // get monthly revenue for last 6 months
            var monthlyRevenueData = await _campRepository.GetMonthlyRevenueAsync(6);
            var monthlyRevenue = monthlyRevenueData.Select(m => new MonthlyRevenueDto
            {
                Month = m.Month,
                Revenue = m.Revenue
            }).ToList();

            return new AdminCampAnalyticsDto
            {
                StatusOverview = statusOverview,
                MonthlyRevenue = monthlyRevenue
            };
        }

        public async Task<AdminPriorityActionsDto> GetPriorityActionsAsync()
        {
            // get pending camps
            var pendingCampsData = await _campRepository.GetPendingCampsAsync();
            var pendingCamps = pendingCampsData.Select(p => new PendingCampDto
            {
                CampId = p.CampId,
                Name = p.Name,
                ManagerName = p.ManagerName,
                SubmittedDate = p.SubmittedDate,
                Status = p.Status
            }).ToList();

            // get recent 10 users
            var recentUsersData = await _userAccountRepository.GetRecentUsersAsync(10);
            var recentUsers = recentUsersData.Select(u => new RecentUserDto
            {
                UserId = u.UserId,
                FullName = u.FullName,
                Email = u.Email,
                Role = u.Role,
                RegisteredDate = u.RegisteredDate
            }).ToList();

            return new AdminPriorityActionsDto
            {
                PendingCamps = pendingCamps,
                RecentUsers = recentUsers
            };
        }
    }
}
