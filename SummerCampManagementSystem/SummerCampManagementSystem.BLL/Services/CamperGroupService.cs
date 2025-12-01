using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.BLL.DTOs.CamperGroup;
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

        public async Task<CamperGroupResponseDto> CreateCamperGroupAsync(CamperGroupRequestDto requestDto)
        {
            // validation
            var camperExists = await _unitOfWork.Campers.GetQueryable().AnyAsync(c => c.camperId == requestDto.camperId);
            if (!camperExists) throw new KeyNotFoundException("Camper not found.");

            var groupExists = await _unitOfWork.Groups.GetQueryable().AnyAsync(g => g.groupId == requestDto.groupId);
            if (!groupExists) throw new KeyNotFoundException("Group not found.");

            var existing = await _unitOfWork.CamperGroups.GetQueryable()
                .FirstOrDefaultAsync(cg => cg.camperId == requestDto.camperId && cg.groupId == requestDto.groupId);
            
            if (existing != null) throw new InvalidOperationException("Camper is already in this group.");

            // create entity
            var camperGroup = _mapper.Map<CamperGroup>(existing);
            camperGroup.status = CamperGroupStatus.Active.ToString();

            await _unitOfWork.CamperGroups.CreateAsync(camperGroup);
            await _unitOfWork.CommitAsync();

            var createdEntity = await _unitOfWork.CamperGroups.GetQueryable()
                .Include(cg => cg.camper)
                .Include(cg => cg.group)
                .FirstOrDefaultAsync(cg => cg.camperGroupId == camperGroup.camperGroupId);

            return _mapper.Map<CamperGroupResponseDto>(createdEntity);
        }

        public async Task<CamperGroupResponseDto> UpdateCamperGroupAsync(int id, CamperGroupRequestDto requestDto)
        {
            var mapping = await _unitOfWork.CamperGroups.GetQueryable()
                .Include(cg => cg.camper)
                .Include(cg => cg.group)
                .FirstOrDefaultAsync(cg => cg.camperGroupId == id);

            if (mapping == null) throw new KeyNotFoundException("CamperGroup mapping not found.");

            // validation
            if (requestDto.groupId.HasValue && requestDto.groupId != mapping.groupId)
            {
                 var groupExists = await _unitOfWork.Groups.GetQueryable().AnyAsync(g => g.groupId == requestDto.groupId);
                 if (!groupExists) throw new KeyNotFoundException("New Group not found.");
                 mapping.groupId = requestDto.groupId.Value;
            }

            if (requestDto.camperId.HasValue && requestDto.camperId != mapping.camperId)
            {
                 var camperExists = await _unitOfWork.Campers.GetQueryable().AnyAsync(c => c.camperId == requestDto.camperId);
                 if (!camperExists) throw new KeyNotFoundException("New Camper not found.");
                 mapping.camperId = requestDto.camperId.Value;
            }

            await _unitOfWork.CamperGroups.UpdateAsync(mapping);
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
            var mapping = await _unitOfWork.CamperGroups.GetByIdAsync(id);
            if (mapping == null) return false;

            mapping.status = CamperGroupStatus.Inactive.ToString();

            await _unitOfWork.CamperGroups.UpdateAsync(mapping);
            await _unitOfWork.CommitAsync();
            return true;
        }
    }
}