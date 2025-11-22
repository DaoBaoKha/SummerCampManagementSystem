using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.Core.Enums;
using SummerCampManagementSystem.DAL.UnitOfWork;
using System;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.Services
{
    public class CampStatusService : ICampStatusService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CampStatusService> _logger;

        public CampStatusService(IUnitOfWork unitOfWork, ILogger<CampStatusService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<bool> TransitionCampStatusSafeAsync(int campId, CampStatus newStatus, string executionSource)
        {
            var camp = await _unitOfWork.Camps.GetByIdAsync(campId);

            if (camp == null)
            {
                _logger.LogWarning($"[{executionSource}] Camp ID {campId} not found. Skipping status transition.");
                return false;
            }

            if (!Enum.TryParse(camp.status, true, out CampStatus currentStatus))
            {
                _logger.LogError($"[{executionSource}] Invalid current status '{camp.status}' for Camp ID {campId}.");
                return false;
            }

            // Check if already in target status (idempotency)
            if (currentStatus == newStatus)
            {
                _logger.LogInformation($"[{executionSource}] Camp ID {campId} is already in status '{newStatus}'. Skipping transition.");
                return true;
            }

            // Validate transition
            if (!IsValidTransition(currentStatus, newStatus))
            {
                _logger.LogWarning($"[{executionSource}] Invalid transition from '{currentStatus}' to '{newStatus}' for Camp ID {campId}. Skipping.");
                return false;
            }

            // Perform transition
            var oldStatus = camp.status;
            camp.status = newStatus.ToString();

            await _unitOfWork.Camps.UpdateAsync(camp);
            await _unitOfWork.CommitAsync();

            _logger.LogInformation($"[{executionSource}] Successfully transitioned Camp ID {campId} ({camp.name}) from '{oldStatus}' to '{newStatus}' at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC.");

            return true;
        }

        private bool IsValidTransition(CampStatus from, CampStatus to)
        {
            return (from, to) switch
            {
                // Published -> OpenForRegistration
                (CampStatus.Published, CampStatus.OpenForRegistration) => true,

                // OpenForRegistration -> RegistrationClosed or UnderEnrolled
                (CampStatus.OpenForRegistration, CampStatus.RegistrationClosed) => true,
                (CampStatus.OpenForRegistration, CampStatus.UnderEnrolled) => true,

                // RegistrationClosed -> InProgress or UnderEnrolled
                (CampStatus.RegistrationClosed, CampStatus.InProgress) => true,
                (CampStatus.RegistrationClosed, CampStatus.UnderEnrolled) => true,

                // UnderEnrolled -> OpenForRegistration or InProgress
                (CampStatus.UnderEnrolled, CampStatus.OpenForRegistration) => true,
                (CampStatus.UnderEnrolled, CampStatus.InProgress) => true,

                // InProgress -> Completed
                (CampStatus.InProgress, CampStatus.Completed) => true,

                // Reject any other transition
                _ => false
            };
        }
    }
}
