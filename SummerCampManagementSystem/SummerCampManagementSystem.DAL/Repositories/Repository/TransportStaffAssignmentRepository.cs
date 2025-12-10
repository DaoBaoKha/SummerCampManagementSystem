using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;
using SummerCampManagementSystem.DAL.Repositories.Repository;

public class TransportStaffAssignmentRepository : GenericRepository<TransportStaffAssignment>, ITransportStaffAssignmentRepository
{
    public TransportStaffAssignmentRepository(CampEaseDatabaseContext context) : base(context) { }

    public async Task<bool> ExistsAsync(int scheduleId, int staffId)
    {
        return await _context.TransportStaffAssignments
            .AnyAsync(x => x.transportScheduleId == scheduleId &&
                           x.staffId == staffId &&
                           x.status == "Active");
    }

    public async Task<IEnumerable<TransportSchedule>> GetSchedulesByStaffIdAsync(int staffId)
    {
        return await _context.TransportStaffAssignments
            .Where(x => x.staffId == staffId && x.status == "Active")
            .Include(x => x.transportSchedule).ThenInclude(ts => ts.route)
            .Include(x => x.transportSchedule).ThenInclude(ts => ts.vehicle)
            .Select(x => x.transportSchedule)
            .OrderByDescending(ts => ts.date)
            .ToListAsync();
    }

    public async Task<IEnumerable<int>> GetBusyStaffIdsInOtherTransportAsync(DateOnly date, TimeOnly start, TimeOnly end)
    {
        var activeStatuses = new[] { "Draft", "NotYet", "InProgress", "Rejected" };

        var list = await _context.TransportStaffAssignments
        .Include(tsa => tsa.transportSchedule)
        .Where(tsa => tsa.status == "Active" &&
                      tsa.staffId != null && // check null
                      tsa.transportSchedule.date == date &&
                      activeStatuses.Contains(tsa.transportSchedule.status) &&
                      tsa.transportSchedule.startTime < end &&
                      tsa.transportSchedule.endTime > start)
        .Select(tsa => tsa.staffId) 
        .Distinct()
        .ToListAsync();

        return list.Where(x => x.HasValue).Select(x => x.Value); 
    }
}