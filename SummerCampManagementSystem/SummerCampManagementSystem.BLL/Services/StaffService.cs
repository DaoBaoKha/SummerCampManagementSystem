using AutoMapper;
using SummerCampManagementSystem.BLL.DTOs.UserAccount;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.Services
{
    public class StaffService : IStaffService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICampStaffAssignmentService _campStaffAssignmentService;
        public StaffService(IUnitOfWork unitOfWork, IMapper mapper, ICampStaffAssignmentService campStaffAssignmentService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _campStaffAssignmentService = campStaffAssignmentService;
        }

        public async Task<IEnumerable<StaffSummaryDto>> GetAvailableActivityStaff(int campId, int activityScheduleId)
        {
            var activity = await _unitOfWork.ActivitySchedules.GetByIdAsync(activityScheduleId)
                ?? throw new KeyNotFoundException("Activity Schedule not found.");

            var staffInCamp = await _campStaffAssignmentService.GetAvailableStaffByCampForActivity(campId);

            var available = new List<StaffSummaryDto>();


            foreach (var staff in staffInCamp)
            {
                if (await _unitOfWork.CamperGroups.isSupervisor(staff.UserId))
                    continue;

                if (await _unitOfWork.ActivitySchedules.IsStaffBusyAsync(
                   staff.UserId,
                   activity.startTime.Value,
                   activity.endTime.Value))
                    continue;

                available.Add(staff);

            }
            return available;
        }
    }
}
