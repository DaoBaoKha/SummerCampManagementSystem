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
        public IPromotionRepository Promotions { get; }
        public IPromotionTypeRepository PromotionTypes { get; }
        public IGuardianRepository Guardians { get; }
        public IActivityRepository Activities { get; }
        public ICamperActivityRepository CamperActivities { get; }
        public IHealthRecordRepository HealthRecords { get; }
        public ITransactionRepository Transactions { get; }
        public ILocationRepository Locations { get; }
        public UnitOfWork(CampEaseDatabaseContext context, IUserRepository userRepository, 
            IRefreshTokenRepository refreshTokenRepository, IVehicleRepository vehicles, 
            IVehicleTypeRepository vehicleTypes, ICampRepository campRepository, ICampTypeRepository campTypes
            ,ICamperGroupRepository camperGroups, IRegistrationRepository registrations, ICamperRepository campers,
            IBlogRepository blogs, IRouteRepository routes, IPromotionTypeRepository promotionTypes,
            IGuardianRepository guardians, IActivityRepository activities, ICamperActivityRepository camperActivities,
            IHealthRecordRepository healthRecords, IPromotionRepository promotions, ITransactionRepository transactions
            ,ILocationRepository locations)
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
            Promotions = promotions;
            PromotionTypes = promotionTypes;
            Guardians = guardians;
            Activities = activities;
            CamperActivities = camperActivities;
            HealthRecords = healthRecords;
            Transactions = transactions;
            Locations = locations;
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
