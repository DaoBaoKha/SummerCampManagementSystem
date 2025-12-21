using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;

namespace SummerCampManagementSystem.DAL.Repositories.Repository
{
    public class CamperGroupRepository : GenericRepository<CamperGroup>, ICamperGroupRepository
    {
        public CamperGroupRepository(CampEaseDatabaseContext context) : base(context)
        {
        }

        public async Task<IEnumerable<int>> GetCamperIdsByGroupIdAsync(int groupId)
        {
            return await _context.Set<CamperGroup>()
                .Where(cg => cg.groupId == groupId)
                .Select(cg => cg.camperId)
                .ToListAsync();
        }

        public async Task<IEnumerable<int>> GetGroupIdsByCamperIdAsync(int camperId)
        {
            return await _context.Set<CamperGroup>()
                .Where(cg => cg.camperId == camperId)
                .Select(cg => cg.groupId)
                .ToListAsync();
        }

        public async Task<bool> IsCamperInGroupAsync(int camperId, int groupId)
        {
            return await _context.Set<CamperGroup>()
                .AnyAsync(cg => cg.camperId == camperId && cg.groupId == groupId);
        }

        public async Task<IEnumerable<Camper>> GetCampersByGroupIdAsync(int groupId)
        {
            return await _context.Set<CamperGroup>()
                .Where(cg => cg.groupId == groupId)
                .Include(cg => cg.camper)
                .Select(cg => cg.camper)
                .ToListAsync();
        }

        public async Task<IEnumerable<CamperGroup>> SearchAsync(int? camperId, int? groupId, int? campId, string? camperName)
        {
            var query = _context.Set<CamperGroup>()
                .Include(cg => cg.camper)
                .Include(cg => cg.group)
                .ThenInclude(g => g.camp)
                .AsNoTracking();

            if (camperId.HasValue)
                query = query.Where(cg => cg.camperId == camperId.Value);

            if (groupId.HasValue)
                query = query.Where(cg => cg.groupId == groupId.Value);

            if (campId.HasValue)
                query = query.Where(cg => cg.group.campId == campId.Value);

            if (!string.IsNullOrEmpty(camperName))
                query = query.Where(cg => cg.camper.camperName.Contains(camperName));

            return await query.ToListAsync();
        }

        public async Task<CamperGroup?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.Set<CamperGroup>()
                .Include(cg => cg.camper)
                .Include(cg => cg.group)
                .FirstOrDefaultAsync(cg => cg.camperGroupId == id);
        }

        public async Task<CamperGroup?> GetByCamperAndGroupAsync(int camperId, int groupId)
        {
            return await _context.Set<CamperGroup>()
                .AsNoTracking()
                .FirstOrDefaultAsync(cg => cg.camperId == camperId && cg.groupId == groupId);
        }

        public async Task<CamperGroup?> GetByIdWithGroupAndCampAsync(int id)
        {
            return await _context.Set<CamperGroup>()
                .Include(cg => cg.camper)
                .Include(cg => cg.group)
                    .ThenInclude(g => g.camp)
                .FirstOrDefaultAsync(cg => cg.camperGroupId == id);
        }
    }
}
