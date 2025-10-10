using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;

namespace SummerCampManagementSystem.DAL.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly CampEaseDatabaseContext _context;

        public IBlogRepository Blogs { get; }
        public IUserRepository Users { get; }
        public IRefreshTokenRepository RefreshTokens { get; }
        public IRegistrationRepository Registrations { get; }
        public IRouteRepository Routes { get; }
        public IVehicleRepository Vehicles { get; }
        public IVehicleTypeRepository VehicleTypes { get; }
        public ICamperGroupRepository CamperGroups { get; }
        public ICampRepository Camps { get; }
        public ICampTypeRepository CampTypes { get; }
        public ICamperRepository Campers { get; }
        public IPaymentRepository Payments { get; }
        public UnitOfWork(CampEaseDatabaseContext context, IUserRepository userRepository, 
            IRefreshTokenRepository refreshTokenRepository, IVehicleRepository vehicles, 
            IVehicleTypeRepository vehicleTypes, ICampRepository campRepository, ICampTypeRepository campTypes
            ,ICamperGroupRepository camperGroups, IRegistrationRepository registrations, ICamperRepository campers,
            IBlogRepository blogs, IRouteRepository routes, IPaymentRepository payments)
        {
            _context = context;
            Blogs = blogs;
            Users = userRepository;
            RefreshTokens = refreshTokenRepository;
            Vehicles = vehicles;
            VehicleTypes = vehicleTypes;
            Camps = campRepository;
            CampTypes = campTypes;
            CamperGroups = camperGroups;
            Campers = campers;
            Registrations = registrations;
            Routes = routes;
            Payments = payments;
        }

        public async Task<int> CommitAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
