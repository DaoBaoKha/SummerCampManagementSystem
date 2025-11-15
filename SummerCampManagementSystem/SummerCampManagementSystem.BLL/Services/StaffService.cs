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

        public async Task<IEnumerable<StaffSummaryDto>> GetAvailableGroupStaffs(int campId)
        {
            var camp = await _unitOfWork.Camps.GetByIdAsync(campId)
                ?? throw new KeyNotFoundException("Camp not found.");

            var staffInCamp = await _campStaffAssignmentService.GetAvailableStaffByCampId(campId);
            var available = new List<StaffSummaryDto>();


            foreach (var staff in staffInCamp)
            {
                if (await _unitOfWork.CamperGroups.isSupervisor(staff.UserId, campId))
                    continue;

                if (await _unitOfWork.ActivitySchedules.IsStaffBusyAsync(
                   staff.UserId,
                   camp.startDate.Value,
                   camp.endDate.Value))
                    continue;

                if (await _unitOfWork.Accommodations.isSupervisorOfAccomodation(staff.UserId, campId))
                    continue;

                available.Add(staff);
            }
            return available;
        }


        public async Task<IEnumerable<StaffSummaryDto>> GetAvailableActivityStaffs(int campId, int activityScheduleId)
        {
            var activitySchedule = await _unitOfWork.ActivitySchedules.GetByIdAsync(activityScheduleId)
                ?? throw new KeyNotFoundException("Activity Schedule not found.");

            var activity = await _unitOfWork.Activities.GetByIdAsync(activitySchedule.activityId)
                ?? throw new KeyNotFoundException("Activity not found.");

            if (activity.campId != campId)
                throw new ArgumentException($"Activity Schedule {activityScheduleId} does not belong to the camp {campId}");

            var staffInCamp = await _campStaffAssignmentService.GetAvailableStaffByCampId(campId);

            var available = new List<StaffSummaryDto>();


            foreach (var staff in staffInCamp)
            {
                if (await _unitOfWork.CamperGroups.isSupervisor(staff.UserId, campId))
                    continue;

                if (await _unitOfWork.ActivitySchedules.IsStaffBusyAsync(
                   staff.UserId,
                   activitySchedule.startTime.Value,
                   activitySchedule.endTime.Value))
                    continue;

                available.Add(staff);

            }
            return available;
        }

        public async Task<IEnumerable<StaffSummaryDto>> GetAvailableAccomodationStaffs(int campId)
        {
            var camp = await _unitOfWork.Camps.GetByIdAsync(campId)
               ?? throw new KeyNotFoundException("Camp not found.");

            var staffInCamp = await _campStaffAssignmentService.GetAvailableStaffByCampId(campId);
            var available = new List<StaffSummaryDto>();


            foreach (var staff in staffInCamp)
            {
                if (await _unitOfWork.Accommodations.isSupervisorOfAccomodation(staff.UserId, campId))
                    continue;

                available.Add(staff);
            }
            return available;
        }



    }
}
