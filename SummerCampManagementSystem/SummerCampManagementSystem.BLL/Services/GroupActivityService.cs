using AutoMapper;
using SummerCampManagementSystem.BLL.DTOs.GroupActivity;
using SummerCampManagementSystem.BLL.Exceptions;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.Core.Enums;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.Services
{
    public class GroupActivityService : IGroupActivityService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GroupActivityService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<GroupActivityResponseDto> CreateGroupActivity(GroupActivityDto groupActivityDto)
        {
            // Validate group exists and load with camp
            var group = await _unitOfWork.Groups.GetByIdWithCampAsync(groupActivityDto.groupId.Value);
            if (group == null)
            {
                throw new NotFoundException($"Không tìm thấy group với ID {groupActivityDto.groupId.Value}");
            }

            // Validate camp status before creating group activity
            if (group.camp != null)
            {
                ValidateCampStatusForGroupActivityOperation(group.camp, "tạo");
            }

            // Validate activity schedule exists
            var activitySchedule = await _unitOfWork.ActivitySchedules.GetScheduleById(groupActivityDto.activityScheduleId.Value);
            if (activitySchedule == null)
            {
                throw new NotFoundException($"Không tìm thấy activity schedule với ID {groupActivityDto.activityScheduleId.Value}");
            }

            // Check if this group is already assigned to this activity schedule
            var existingGroupActivity = await _unitOfWork.GroupActivities
                .GetByGroupAndActivityScheduleId(groupActivityDto.groupId.Value, groupActivityDto.activityScheduleId.Value);
            
            if (existingGroupActivity != null)
            {
                throw new BusinessRuleException($"Group với id {group.groupId} đã được gán vào activity schedule này rồi.");
            }

            // Create new group activity
            var groupActivity = _mapper.Map<GroupActivity>(groupActivityDto);

            await _unitOfWork.GroupActivities.CreateAsync(groupActivity);
            await _unitOfWork.CommitAsync();

            return _mapper.Map<GroupActivityResponseDto>(groupActivity);
        }

        public async Task<bool> RemoveGroupActivity(int groupActivityId)
        {
            var groupActivity = await _unitOfWork.GroupActivities.GetByIdWithGroupAndCampAsync(groupActivityId);

            if (groupActivity == null)
            {
                return false;
            }

            // Validate camp status before deleting group activity
            if (groupActivity.group?.camp != null)
            {
                ValidateCampStatusForGroupActivityOperation(groupActivity.group.camp, "xóa");
            }

           await _unitOfWork.GroupActivities.RemoveAsync(groupActivity);
           await _unitOfWork.CommitAsync();
           return true;
        }

        #region Private Methods

        private void ValidateCampStatusForGroupActivityOperation(Camp camp, string operation)
        {
            var campStatus = camp.status;

            // Block operations if camp status is RegistrationClosed or later
            if (campStatus == CampStatus.RegistrationClosed.ToString() ||
                campStatus == CampStatus.UnderEnrolled.ToString() ||
                campStatus == CampStatus.InProgress.ToString() ||
                campStatus == CampStatus.Completed.ToString() ||
                campStatus == CampStatus.Canceled.ToString())
            {
                throw new BadRequestException($"Không thể {operation} group activity khi trại đã ở trạng thái '{campStatus}'. Trại phải ở trạng thái Draft, PendingApproval, Rejected, Published, hoặc OpenForRegistration.");
            }
        }

        #endregion
    }
}
