using AutoMapper;
using SummerCampManagementSystem.BLL.DTOs.Dashboard;
using SummerCampManagementSystem.BLL.Exceptions;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;

namespace SummerCampManagementSystem.BLL.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IRegistrationRepository _registrationRepository;
        private readonly ICamperRepository _camperRepository;
        private readonly IGroupRepository _groupRepository;
        private readonly IAccommodationRepository _accommodationRepository;
        private readonly ICampRepository _campRepository;
        private readonly IRegistrationCamperRepository _registrationCamperRepository;

        public DashboardService(
            IRegistrationRepository registrationRepository, ICamperRepository camperRepository, IGroupRepository groupRepository, IAccommodationRepository accommodationRepository, ICampRepository campRepository, IRegistrationCamperRepository registrationCamperRepository)
        {
            _registrationRepository = registrationRepository;
            _camperRepository = camperRepository;
            _groupRepository = groupRepository;
            _accommodationRepository = accommodationRepository;
            _campRepository = campRepository;
            _registrationCamperRepository = registrationCamperRepository;
        }

        public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(int campId)
        {
            // validate camp exists
            var camp = await _campRepository.GetByIdAsync(campId);
            if (camp == null)
            {
                throw new NotFoundException($"Camp with ID {campId} not found");
            }

            return await GetSummaryDataAsync(campId, camp.maxParticipants ?? 0);
        }

        public async Task<DashboardAnalyticsDto> GetDashboardAnalyticsAsync(int campId)
        {
            // validate camp exists
            var camp = await _campRepository.GetByIdAsync(campId);
            if (camp == null)
            {
                throw new NotFoundException($"Camp with ID {campId} not found");
            }

            return await GetAnalyticsDataAsync(campId);
        }

        public async Task<DashboardOperationsDto> GetDashboardOperationsAsync(int campId)
        {
            // validate camp exists
            var camp = await _campRepository.GetByIdAsync(campId);
            if (camp == null)
            {
                throw new NotFoundException($"Camp with ID {campId} not found");
            }

            return await GetOperationsDataAsync(campId);
        }

        private async Task<DashboardSummaryDto> GetSummaryDataAsync(int campId, int maxParticipants)
        {
            // get total revenue
            var totalRevenue = await _registrationRepository.GetTotalRevenueAsync(campId);

            // get total campers (with confirmed statuses)
            // include PendingAssignGroup because these campers have paid and are active participants
            var validStatuses = new[] { "PendingAssignGroup", "Confirmed", "Transporting", "Transported", "CheckedIn", "CheckedOut" };
            var campRegistrationCampers = await _registrationCamperRepository.GetByCampIdAsync(campId);
            var confirmedCampers = campRegistrationCampers
                .Where(rc => validStatuses.Contains(rc.status))
                .Select(rc => rc.camperId)
                .Distinct()
                .Count();

            // get pending approvals
            var pendingApprovals = await _registrationRepository.GetPendingApprovalsCountAsync(campId);

            // get cancellation rate
            var cancellationRate = await _registrationRepository.GetCancellationRateAsync(campId);

            // calculate occupancy
            var occupancyPercentage = maxParticipants > 0 ? (double)confirmedCampers / maxParticipants * 100 : 0;

            return new DashboardSummaryDto
            {
                TotalRevenue = totalRevenue,
                TotalCampers = confirmedCampers,
                PendingApprovals = pendingApprovals,
                CancellationRate = Math.Round(cancellationRate, 2),
                Occupancy = new OccupancyDto
                {
                    Current = confirmedCampers,
                    Max = maxParticipants,
                    Percentage = Math.Round(occupancyPercentage, 2)
                }
            };
        }

        private async Task<DashboardAnalyticsDto> GetAnalyticsDataAsync(int campId)
        {
            // get registration trend
            var trendData = await _registrationRepository.GetRegistrationTrendAsync(campId);
            var registrationTrend = trendData.Select(t => new RegistrationTrendDto
            {
                Date = t.Date.ToString("yyyy-MM-dd"),
                Count = t.Count,
                Revenue = t.Revenue
            }).ToList();

            // get status distribution
            var statusDistribution = await _registrationRepository.GetStatusDistributionAsync(campId);

            // get camper profile (demographics)
            var (genderDict, ageGroupsDict) = await _camperRepository.GetCamperProfileByCampAsync(campId);
            var camperProfile = new CamperProfileDto
            {
                Gender = genderDict,
                AgeGroups = ageGroupsDict.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value)
            };

            return new DashboardAnalyticsDto
            {
                RegistrationTrend = registrationTrend,
                StatusDistribution = statusDistribution,
                CamperProfile = camperProfile
            };
        }

        private async Task<DashboardOperationsDto> GetOperationsDataAsync(int campId)
        {
            // get capacity alerts from groups
            var groupAlerts = await _groupRepository.GetCapacityAlertsByCampAsync(campId);
            var groupCapacityAlerts = groupAlerts.Select(g => new CapacityAlertDto
            {
                Type = "Group",
                Name = g.Name,
                Current = g.Current,
                Max = g.Max
            }).ToList();

            // get capacity alerts from accommodations
            var accommodationAlerts = await _accommodationRepository.GetCapacityAlertsByCampAsync(campId);
            var accommodationCapacityAlerts = accommodationAlerts.Select(a => new CapacityAlertDto
            {
                Type = "Accommodation",
                Name = a.Name,
                Current = a.Current,
                Max = a.Max
            }).ToList();

            // combine all capacity alerts
            var allCapacityAlerts = groupCapacityAlerts.Concat(accommodationCapacityAlerts).ToList();

            // get recent registrations
            var recentRegistrationsData = await _registrationRepository.GetRecentRegistrationsAsync(campId, 5);
            var recentRegistrations = recentRegistrationsData.Select(r => new RecentRegistrationDto
            {
                RegistrationId = r.RegistrationId,
                CamperName = r.CamperName,
                RegistrationDate = r.RegistrationDate,
                Status = r.Status,
                Amount = r.Amount,
                Avatar = r.Avatar
            }).ToList();

            return new DashboardOperationsDto
            {
                CapacityAlerts = allCapacityAlerts,
                RecentRegistrations = recentRegistrations
            };
        }
    }
}
