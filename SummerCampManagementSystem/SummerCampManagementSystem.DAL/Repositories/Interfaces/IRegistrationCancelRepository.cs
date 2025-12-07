using SummerCampManagementSystem.DAL.Models;

namespace SummerCampManagementSystem.DAL.Repositories.Interfaces
{
    public interface IRegistrationCancelRepository : IGenericRepository<RegistrationCancel>
    {
        Task<RegistrationCancel?> GetByIdWithDetailsAsync(int id);

        IQueryable<RegistrationCancel> GetQueryableWithDetails();
    }
}
