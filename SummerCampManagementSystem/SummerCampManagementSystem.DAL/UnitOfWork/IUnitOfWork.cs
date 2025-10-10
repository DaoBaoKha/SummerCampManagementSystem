using SummerCampManagementSystem.DAL.Repositories.Interfaces;

namespace SummerCampManagementSystem.DAL.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        IBlogRepository Blogs { get; }
        IUserRepository Users { get; }
        IRefreshTokenRepository RefreshTokens { get; }
        IRegistrationRepository Registrations { get; }
        IRouteRepository Routes { get; }
        IVehicleRepository Vehicles { get; }
        IVehicleTypeRepository VehicleTypes { get; }
        ICamperGroupRepository CamperGroups { get; }
        ICampRepository Camps { get; }
        ICampTypeRepository CampTypes { get; }
        ICamperRepository Campers { get; }
        IPaymentRepository Payments { get; }
        Task<int> CommitAsync();
    }
}
