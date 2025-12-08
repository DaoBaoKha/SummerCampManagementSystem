using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.BLL.DTOs.TransportStaffAssignment;
using SummerCampManagementSystem.BLL.Exceptions;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.Core.Enums;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;

namespace SummerCampManagementSystem.BLL.Services
{
    public class TransportStaffAssignmentService : ITransportStaffAssignmentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public TransportStaffAssignmentService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<TransportStaffAssignmentResponseDto>> SearchAssignmentsAsync(TransportStaffAssignmentSearchDto searchDto)
        {
            var query = _unitOfWork.TransportStaffAssignments.GetQueryable()
                .Include(x => x.staff)
                .Include(x => x.transportSchedule)
                .AsNoTracking();

            if (searchDto.TransportScheduleId.HasValue)
                query = query.Where(x => x.transportScheduleId == searchDto.TransportScheduleId);

            if (searchDto.StaffId.HasValue)
                query = query.Where(x => x.staffId == searchDto.StaffId);


            // get status = active if no data input
            if (!string.IsNullOrEmpty(searchDto.Status))
            {
                query = query.Where(x => x.status == searchDto.Status);
            }
            else
            {
                query = query.Where(x => x.status == "Active");
            }

            var entities = await query.ToListAsync();
            return _mapper.Map<IEnumerable<TransportStaffAssignmentResponseDto>>(entities);
        }

        public async Task<TransportStaffAssignmentResponseDto> AssignStaffAsync(TransportStaffAssignmentCreateDto dto)
        {
            var schedule = await _unitOfWork.TransportSchedules.GetByIdAsync(dto.TransportScheduleId)
                ?? throw new NotFoundException($"Không tìm thấy lịch trình vận chuyển ID {dto.TransportScheduleId}.");

            // validation
            await ValidateAssignmentRules(dto.StaffId, schedule);

            var assignment = _mapper.Map<TransportStaffAssignment>(dto);
            assignment.status = "Active";

            await _unitOfWork.TransportStaffAssignments.CreateAsync(assignment);
            await _unitOfWork.CommitAsync();

            var createdEntity = await _unitOfWork.TransportStaffAssignments.GetQueryable()
                .Include(x => x.staff)
                .FirstOrDefaultAsync(x => x.transportStaffAssignmentId == assignment.transportStaffAssignmentId);

            return _mapper.Map<TransportStaffAssignmentResponseDto>(createdEntity);
        }

        public async Task<TransportStaffAssignmentResponseDto> UpdateAssignmentAsync(int id, TransportStaffAssignmentUpdateDto dto)
        {
            var existing = await _unitOfWork.TransportStaffAssignments.GetByIdAsync(id)
                ?? throw new NotFoundException($"Không tìm thấy phân công ID {id}.");

            var schedule = await _unitOfWork.TransportSchedules.GetByIdAsync((int)dto.TransportScheduleId)
                ?? throw new NotFoundException($"Không tìm thấy lịch trình vận chuyển ID {dto.TransportScheduleId}.");

            // validation
            await ValidateAssignmentRules((int)dto.StaffId, schedule);

            _mapper.Map(dto, existing);

            await _unitOfWork.TransportStaffAssignments.UpdateAsync(existing);
            await _unitOfWork.CommitAsync();

            var updatedEntity = await _unitOfWork.TransportStaffAssignments.GetQueryable()
               .Include(x => x.staff)
               .FirstOrDefaultAsync(x => x.transportStaffAssignmentId == id);

            return _mapper.Map<TransportStaffAssignmentResponseDto>(updatedEntity);
        }

        public async Task<bool> DeleteAssignmentAsync(int id)
        {
            var existing = await _unitOfWork.TransportStaffAssignments.GetByIdAsync(id)
                 ?? throw new NotFoundException($"Không tìm thấy phân công ID {id}.");

            // soft delete
            existing.status = "Inactive";

            await _unitOfWork.TransportStaffAssignments.UpdateAsync(existing);
            await _unitOfWork.CommitAsync();
            return true;
        }


        #region Private Methods

        private async Task ValidateAssignmentRules(int staffId, TransportSchedule schedule)
        {
            // validation
            if (!schedule.date.HasValue || !schedule.startTime.HasValue || !schedule.endTime.HasValue)
                throw new BusinessRuleException("Lịch trình vận chuyển chưa có đầy đủ thông tin ngày giờ.");

            if (schedule.status == TransportScheduleStatus.Completed.ToString() ||
                schedule.status == TransportScheduleStatus.Canceled.ToString())
            {
                throw new BusinessRuleException($"Không thể gán nhân viên vào lịch trình đang ở trạng thái '{schedule.status}'.");
            }

            var staffUser = await _unitOfWork.Users.GetByIdAsync(staffId);
            if (staffUser == null)
                throw new NotFoundException($"Không tìm thấy nhân viên ID {staffId}.");

            if (staffUser.role != UserRole.Staff.ToString() && staffUser.role != UserRole.Manager.ToString())
            {
                throw new BusinessRuleException("Người được phân công phải có quyền Staff hoặc Manager.");
            }

            // check if staff already in the transport schedule
            bool existsInSchedule = await _unitOfWork.TransportStaffAssignments
                .ExistsAsync(schedule.transportScheduleId, staffId);

            if (existsInSchedule)
            {
                throw new BusinessRuleException("Nhân viên này đã được phân công vào chuyến xe này rồi.");
            }

            // check transport conflict
            bool isBusyTransport = await _unitOfWork.TransportStaffAssignments
                .IsStaffAvailableAsync(staffId, schedule.date.Value, schedule.startTime.Value, schedule.endTime.Value);

            if (isBusyTransport)
            {
                throw new BusinessRuleException($"Staff ID {staffId} đang bận đi một chuyến xe khác trong khung giờ này ({schedule.startTime} - {schedule.endTime}).");
            }

            // check activity conflict
            bool isBusyActivity = await _unitOfWork.ActivitySchedules.IsStaffBusyAsync(
                 staffId,
                 schedule.date.Value.ToDateTime(schedule.startTime.Value),
                 schedule.date.Value.ToDateTime(schedule.endTime.Value)
            );

            if (isBusyActivity)
            {
                throw new BusinessRuleException($"Staff ID {staffId} đang có lịch tham gia Activity (Hoạt động) tại trại trong khung giờ này.");
            }

            // check campStaffAssignment conflict
            // only not allowed if the camp is ongoing
            bool isBusyAtCamp = await _unitOfWork.CampStaffAssignments
                .IsStaffBusyInAnyCampAsync(staffId, schedule.date.Value);

            if (isBusyAtCamp)
            {
                throw new BusinessRuleException($"Staff ID {staffId} hiện đang có lịch làm việc tại một Trại đang diễn ra vào ngày {schedule.date}, nên không thể phân công đi xe.");
            }
        }

        #endregion
    }
}