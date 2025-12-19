using SummerCampManagementSystem.DAL.Models;

namespace SummerCampManagementSystem.DAL.Repositories.Interfaces
{
    public interface ICamperAccommodationRepository : IGenericRepository<CamperAccommodation>
    {
        Task<IEnumerable<CamperAccommodation>> SearchAsync(int? camperId, int? accommodationId, int? campId, string? camperName);
        Task<CamperAccommodation?> GetByIdWithDetailsAsync(int id);
        Task<CamperAccommodation?> GetByCamperAndAccommodationAsync(int camperId, int accommodationId);
        Task<CamperAccommodation?> GetByIdWithAccommodationAndCampAsync(int id);
        Task<bool> IsAccommodationStaffOfCamper(int staffId, int camperId);
    }
}
