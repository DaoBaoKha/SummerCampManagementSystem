using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.BLL.DTOs.CamperGroup;
using SummerCampManagementSystem.BLL.DTOs.RegistrationCamper;
using SummerCampManagementSystem.BLL.Exceptions;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.Core.Enums;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;

namespace SummerCampManagementSystem.BLL.Services
{
    public class CamperGroupService : ICamperGroupService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CamperGroupService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<CamperGroupResponseDto>> GetCamperGroupsAsync(CamperGroupSearchDto searchDto)
        {
            var query = _unitOfWork.CamperGroups.GetQueryable()
                .Include(cg => cg.camper)
                .Include(cg => cg.group)
                .ThenInclude(g => g.camp)
                .AsNoTracking();

            if (searchDto.CamperId.HasValue)
            {
                query = query.Where(cg => cg.camperId == searchDto.CamperId.Value);
            }

            if (searchDto.GroupId.HasValue)
            {
                query = query.Where(cg => cg.groupId == searchDto.GroupId.Value);
            }

            if (searchDto.CampId.HasValue)
            {
                query = query.Where(cg => cg.group.campId == searchDto.CampId.Value);
            }

            if (searchDto.CamperName != null)
            {
                query = query.Where(cg => cg.camper.camperName.Contains(searchDto.CamperName));
            }

            var entities = await query.ToListAsync();

            return _mapper.Map<IEnumerable<CamperGroupResponseDto>>(entities);
        }

        public async Task<IEnumerable<RegistrationCamperResponseDto>> GetPendingAssignCampersAsync(int? campId)
        {
            IQueryable<RegistrationCamper> query = _unitOfWork.RegistrationCampers.GetQueryable()
                .Where(rc => rc.status == RegistrationCamperStatus.PendingAssignGroup.ToString())
                .Include(rc => rc.camper)
                .Include(rc => rc.registration)
                    .ThenInclude(r => r.camp);

            if (campId.HasValue)
            {
                query = query.Where(rc => rc.registration.campId == campId.Value);
            }

            var entities = await query.ToListAsync();

            return _mapper.Map<IEnumerable<RegistrationCamperResponseDto>>(entities);
        }


        public async Task<CamperGroupResponseDto> CreateCamperGroupAsync(CamperGroupRequestDto requestDto)
        {
            if (!requestDto.camperId.HasValue || !requestDto.groupId.HasValue)
                throw new BadRequestException("CamperId và GroupId không được để trống.");

            var camper = await _unitOfWork.Campers.GetByIdAsync(requestDto.camperId.Value)
                ?? throw new NotFoundException($"Camper ID {requestDto.camperId} not found.");

            var group = await _unitOfWork.Groups.GetQueryable()
                .Include(g => g.CamperGroups) // include to check maxSize
                .FirstOrDefaultAsync(g => g.groupId == requestDto.groupId.Value)
                ?? throw new NotFoundException($"Group ID {requestDto.groupId} not found.");

            // check if camper already in the group
            var existing = await _unitOfWork.CamperGroups.GetQueryable()
                .FirstOrDefaultAsync(cg => cg.camperId == requestDto.camperId && cg.groupId == requestDto.groupId);
            if (existing != null)
                throw new BusinessRuleException($"Camper {camper.camperName} đã thuộc nhóm {group.groupName}.");

            // check camper status 
            var regCamper = await GetRegistrationCamperAsync(camper.camperId, (int)group.campId);
            if (regCamper == null)
                throw new BusinessRuleException($"Camper chưa đăng ký hoặc chưa hoàn tất thanh toán cho Camp {group.campId}.");

            if (regCamper.status != RegistrationCamperStatus.Confirmed.ToString() &&
                regCamper.status != RegistrationCamperStatus.PendingAssignGroup.ToString())
            {
                throw new BusinessRuleException($"Không thể gán nhóm. Trạng thái Camper là '{regCamper.status}'.");
            }

            // validation
            ValidateGroupConstraints(camper, group);

            var camperGroup = new CamperGroup
            {
                camperId = requestDto.camperId.Value,
                groupId = requestDto.groupId.Value,
                status = CamperGroupStatus.Active.ToString()
            };

            await _unitOfWork.CamperGroups.CreateAsync(camperGroup);

            // update currentSize and RegistrationCamper status
            group.currentSize = (group.currentSize ?? 0) + 1;
            regCamper.status = RegistrationCamperStatus.Confirmed.ToString(); 

            await _unitOfWork.Groups.UpdateAsync(group);
            await _unitOfWork.RegistrationCampers.UpdateAsync(regCamper);

            await _unitOfWork.CommitAsync();

            var createdEntity = await _unitOfWork.CamperGroups.GetQueryable()
                .Include(cg => cg.camper)
                .Include(cg => cg.group)
                .FirstOrDefaultAsync(cg => cg.camperGroupId == camperGroup.camperGroupId);

            return _mapper.Map<CamperGroupResponseDto>(createdEntity);
        }

        public async Task<CamperGroupResponseDto> UpdateCamperGroupAsync(int id, CamperGroupRequestDto requestDto)
        {
            if (!requestDto.groupId.HasValue)
                throw new BadRequestException("GroupId không được để trống khi cập nhật.");

            var currentMapping = await _unitOfWork.CamperGroups.GetQueryable()
                .Include(cg => cg.camper)
                .Include(cg => cg.group) // get old group
                .FirstOrDefaultAsync(cg => cg.camperGroupId == id);

            if (currentMapping == null) throw new NotFoundException("Không tìm thấy thông tin CamperGroup.");

            var oldGroup = currentMapping.group;
            var newGroup = await _unitOfWork.Groups.GetQueryable()
                .Include(g => g.CamperGroups) // include to check new group currentSize
                .FirstOrDefaultAsync(g => g.groupId == requestDto.groupId.Value)
                ?? throw new NotFoundException("Không tìm thấy nhóm mới.");

            if (oldGroup.groupId == newGroup.groupId)
                throw new BusinessRuleException("Camper đã thuộc nhóm này rồi.");

            // validation
            ValidateGroupConstraints(currentMapping.camper, newGroup);

            currentMapping.groupId = newGroup.groupId;

            // update currentSize
            oldGroup.currentSize = (oldGroup.currentSize ?? 0) - 1;
            newGroup.currentSize = (newGroup.currentSize ?? 0) + 1;

            await _unitOfWork.CamperGroups.UpdateAsync(currentMapping);
            await _unitOfWork.Groups.UpdateAsync(oldGroup);
            await _unitOfWork.Groups.UpdateAsync(newGroup);

            await _unitOfWork.CommitAsync();

            var updatedEntity = await _unitOfWork.CamperGroups.GetQueryable()
                .Include(cg => cg.camper)
                .Include(cg => cg.group)
                .AsNoTracking()
                .FirstOrDefaultAsync(cg => cg.camperGroupId == id);

            return _mapper.Map<CamperGroupResponseDto>(updatedEntity);
        }

        public async Task<bool> DeleteCamperGroupAsync(int id)
        {
            var mapping = await _unitOfWork.CamperGroups.GetQueryable()
                  .Include(cg => cg.group)
                  .FirstOrDefaultAsync(cg => cg.camperGroupId == id);

            if (mapping == null) return false;

            var group = mapping.group;

            // decrease old group currentSize
            group.currentSize = (group.currentSize ?? 0) - 1;

            // update CamperRegistration status = PendingAssignGroup
            var regCamper = await GetRegistrationCamperAsync(mapping.camperId, (int)group.campId);
            if (regCamper != null)
            {
                regCamper.status = RegistrationCamperStatus.PendingAssignGroup.ToString();
                await _unitOfWork.RegistrationCampers.UpdateAsync(regCamper);
            }

            // soft delete
            mapping.status = CamperGroupStatus.Inactive.ToString();

            await _unitOfWork.CamperGroups.UpdateAsync(mapping);
            await _unitOfWork.Groups.UpdateAsync(group);
            await _unitOfWork.CommitAsync();

            return true;
        }

        #region Private Methods

        private async Task<RegistrationCamper?> GetRegistrationCamperAsync(int camperId, int campId)
        {
            return await _unitOfWork.RegistrationCampers.GetQueryable()
                .Include(rc => rc.registration)
                .FirstOrDefaultAsync(rc => rc.camperId == camperId && rc.registration.campId == campId);
        }

        private void ValidateGroupConstraints(Camper camper, Group group)
        {
            // check age
            if (!camper.dob.HasValue)
                throw new BusinessRuleException($"Không thể gán nhóm. Camper {camper.camperName} chưa có thông tin ngày sinh.");

            var today = DateOnly.FromDateTime(DateTime.Now);
            int age = today.Year - camper.dob.Value.Year;
            if (today < camper.dob.Value.AddYears(age)) age--;

            if (age < group.minAge || age > group.maxAge)
            {
                throw new BusinessRuleException($"Không thể gán nhóm. Tuổi của Camper ({age}) nằm ngoài phạm vi cho phép ({group.minAge}-{group.maxAge}).");
            }

            // check capacity
            // get current size from memory (CamperGroups.Count)
            int currentSize = group.CamperGroups?.Count ?? 0;

            if (group.maxSize.HasValue && group.maxSize.Value > 0 && currentSize >= group.maxSize.Value)
            {
                throw new BusinessRuleException($"Không thể gán nhóm. Nhóm {group.groupName} đã đầy (Sĩ số tối đa: {group.maxSize}).");
            }
        }

        #endregion
    }
}