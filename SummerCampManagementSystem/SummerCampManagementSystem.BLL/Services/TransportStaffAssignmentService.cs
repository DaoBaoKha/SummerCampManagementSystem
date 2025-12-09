using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.BLL.DTOs.TransportStaffAssignment;
using SummerCampManagementSystem.BLL.DTOs.UserAccount;
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

        public async Task<IEnumerable<StaffSummaryDto>> GetAvailableStaffForScheduleAsync(int transportScheduleId)
        {
            // validation
            var schedule = await _unitOfWork.TransportSchedules.GetByIdAsync(transportScheduleId)
                ?? throw new NotFoundException($"Không tìm thấy lịch trình vận chuyển ID {transportScheduleId}.");

            if (!schedule.campId.HasValue || !schedule.date.HasValue || !schedule.startTime.HasValue || !schedule.endTime.HasValue)
                throw new BusinessRuleException("Lịch trình thiếu thông tin ngày giờ.");

            var startUtc = schedule.date.Value.ToDateTime(schedule.startTime.Value);
            var endUtc = schedule.date.Value.ToDateTime(schedule.endTime.Value);

            // get all staff in this camp
            var candidateIds = (await _unitOfWork.CampStaffAssignments
                .GetStaffIdsByCampIdAsync(schedule.campId.Value)).ToList();

            if (!candidateIds.Any()) return new List<StaffSummaryDto>();


            // get id of unavailable staff
            // staff already in the transport
            var assignedToThisScheduleIds = await _unitOfWork.TransportStaffAssignments.GetQueryable()
                .Where(x => x.transportScheduleId == transportScheduleId && x.status == "Active")
                .Select(x => x.staffId)
                .ToListAsync();

            // safe convert to List<int>
            var assignedToThisScheduleIdsSafe = assignedToThisScheduleIds.Where(x => x.HasValue).Select(x => x.Value).ToList();

            // get staff busy in other transport
            var busyInOtherTransportIds = await _unitOfWork.TransportStaffAssignments
                .GetBusyStaffIdsInOtherTransportAsync(schedule.date.Value, schedule.startTime.Value, schedule.endTime.Value);

            var busyInActivityIds = await _unitOfWork.ActivitySchedules
                .GetBusyStaffIdsInActivityAsync(startUtc, endUtc);

            // staff busy in other camp
            var busyInOtherCampIds = await _unitOfWork.CampStaffAssignments
                .GetBusyStaffIdsInOtherActiveCampAsync(startUtc, schedule.campId.Value);

            // include all busy IDs into a HashSet for fast lookup
            // everything return as IEnumerable<int> or List<int> -> HashSet show no errors
            var allBusyIds = new HashSet<int>(assignedToThisScheduleIdsSafe);
            foreach (var id in busyInOtherTransportIds) allBusyIds.Add(id);
            foreach (var id in busyInActivityIds) allBusyIds.Add(id);
            foreach (var id in busyInOtherCampIds) allBusyIds.Add(id);

            // exclude busy staff from list
            var availableStaffIds = candidateIds.Where(id => !allBusyIds.Contains(id)).ToList();

            if (!availableStaffIds.Any()) return new List<StaffSummaryDto>();

            var users = await _unitOfWork.Users.GetQueryable()
                .Where(u => availableStaffIds.Contains(u.userId))
                .ToListAsync(); 

            return _mapper.Map<IEnumerable<StaffSummaryDto>>(users);
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

            var startUtc = schedule.date.Value.ToDateTime(schedule.startTime.Value);
            var endUtc = schedule.date.Value.ToDateTime(schedule.endTime.Value);

            // check duplicate with other transport
            var busyTransportIds = await _unitOfWork.TransportStaffAssignments
                 .GetBusyStaffIdsInOtherTransportAsync(schedule.date.Value, schedule.startTime.Value, schedule.endTime.Value);

            if (busyTransportIds.Contains(staffId))
                throw new BusinessRuleException($"Nhân viên bị trùng lịch với chuyến xe khác trong khung giờ {schedule.startTime} - {schedule.endTime}.");

            // if assign staff to the same camp -> check if staff busy with activity
            if (await _unitOfWork.ActivitySchedules.IsStaffBusyAsync(staffId, startUtc, endUtc))
                throw new BusinessRuleException($"Nhân viên đang có lịch hoạt động (Activity) tại trại vào khung giờ {schedule.startTime} - {schedule.endTime}.");

            // if staff working for another cmap at the same DATE -> cant assign
            if (await _unitOfWork.CampStaffAssignments.IsStaffBusyInOtherActiveCampAsync(staffId, startUtc, schedule.campId.Value))
                throw new BusinessRuleException($"Nhân viên đang thuộc biên chế làm việc tại một trại khác (Active) trong ngày {schedule.date}, nên không thể phân công đi xe.");
        }

        #endregion
    }
}