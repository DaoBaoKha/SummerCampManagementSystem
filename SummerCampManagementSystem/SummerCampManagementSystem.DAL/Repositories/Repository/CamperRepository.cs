using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.DAL.Repositories.Repository
{
    public class CamperRepository : GenericRepository<Camper>, ICamperRepository
    {
        public CamperRepository(CampEaseDatabaseContext context) : base(context)
        {
            _context = context;
        }

        public new async Task<IEnumerable<Camper>> GetAllAsync()
        {
            return await _context.Campers
                .Include(c => c.HealthRecord) 
                .Include(c => c.CamperGroups)
                .ToListAsync();
        }

        public new async Task<Camper?> GetByIdAsync(int id)
        {
            return await _context.Campers
                .Include(c => c.HealthRecord) 
                .FirstOrDefaultAsync(c => c.camperId == id);
        }

        public async Task<IEnumerable<Camper>> GetGuardiansByCamperId(int camperId)
        {
            var camper = await _context.Campers
                .Include(c => c.CamperGuardians)
                .ThenInclude(cg => cg.guardian)
                .Where(c => c.camperId == camperId)
                .ToListAsync();
            return camper;
        }
        public async Task<Registration?> GetRegistrationByCamperIdAsync(int camperId)
        {
            // FIX: Truy vấn thông qua bảng trung gian RegistrationCampers để lấy Registration
            var registrationCamperLink = await _context.RegistrationCampers
                .Include(rc => rc.registration) // Eager load the Registration object
                .FirstOrDefaultAsync(rc => rc.camperId == camperId);

            // Trả về đối tượng Registration từ liên kết
            return registrationCamperLink?.registration;
        }


        public async Task<bool> IsStaffSupervisorOfCamperAsync(int staffId, int camperId)
        {
            return await _context.Campers
                .Where(c => c.camperId == camperId)
                .SelectMany(c => c.CamperGroups)
                .AnyAsync(cg => cg.group.supervisorId == staffId); 
        }

        public async Task<IEnumerable<Camper>> GetCampersByOptionalActivityId(int optionalActivityId)
        {
            return await _context.Campers
                .Where(c => c.CamperActivities.Any(oa => oa.activityScheduleId == optionalActivityId))
                .ToListAsync();
        }

        public async Task<IEnumerable<Camper>> GetCampersByCoreScheduleAndStaffAsync(int activityScheduleId, int staffId)
        {
            return await _context.GroupActivities
                .Where(ga => ga.activityScheduleId == activityScheduleId
                             && ga.group.supervisorId == staffId)
                .SelectMany(ga => ga.group.CamperGroups) 
                .Select(cg => cg.camper) 
                .Distinct() 
                .ToListAsync();
        }

        public async Task<IEnumerable<Camper>> GetCampersByAccommodationScheduleAndStaffAsync(int activityScheduleId, int staffId)
        {
            return await _context.AccommodationActivitySchedules
                  .Where(ga => ga.activityScheduleId == activityScheduleId
                               && ga.accommodation.supervisorId == staffId)
                  .SelectMany(ga => ga.accommodation.CamperAccommodations)
                  .Select(cg => cg.camper)
                  .Distinct()   
                  .ToListAsync();
        }
        public async Task<IEnumerable<Camper>> GetCampersByCoreScheduleIdAsync(int activityScheduleId)
        {
            return await _context.GroupActivities
                .Where(ga => ga.activityScheduleId == activityScheduleId)
                .SelectMany(ga => ga.group.CamperGroups)
                .Select(cg => cg.camper) 
                .Distinct()
                .ToListAsync();
        }

        public async Task<IEnumerable<Camper>> GetCampersByAccommodationScheduleAsync(int activityScheduleId)
        {
            return await _context.AccommodationActivitySchedules
                  .Where(ga => ga.activityScheduleId == activityScheduleId)
                  .SelectMany(ga => ga.accommodation.CamperAccommodations)
                  .Select(cg => cg.camper)
                  .Distinct()
                  .ToListAsync();
        }
    }
}
