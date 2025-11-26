using Microsoft.EntityFrameworkCore.Storage;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;

namespace SummerCampManagementSystem.DAL.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly CampEaseDatabaseContext _context;
        public IAccommodationRepository Accommodations { get; }
        public IAccommodationTypeRepository AccommodationTypes { get; }
        public IActivityRepository Activities { get; }
        public IActivityScheduleRepository ActivitySchedules { get; }
        public IAlbumRepository Albums { get; }
        public IAlbumPhotoRepository AlbumPhotos { get; }
        public IAlbumPhotoFaceRepository AlbumPhotoFaces { get; }
        public IBlogRepository Blogs { get; }
        public IUserRepository Users { get; }
        public IUserAccountRepository UserAccounts { get; }
        public IRefreshTokenRepository RefreshTokens { get; }
        public IRegistrationRepository Registrations { get; }
        public IRegistrationOptionalActivityRepository RegistrationOptionalActivities { get; }
        public IReportRepository Reports { get; }
        public IRouteRepository Routes { get; }
        public IRouteStopRepository RouteStops { get; }
        public IVehicleRepository Vehicles { get; }
        public IVehicleTypeRepository VehicleTypes { get; }
        public ICamperGroupRepository CamperGroups { get; }
        public ICampRepository Camps { get; }
        public ICampTypeRepository CampTypes { get; }
        public ICamperRepository Campers { get; }
        public ICampStaffAssignmentRepository CampStaffAssignments { get; }
        public IChatConversationRepository ChatConversations { get; }
        public IChatMessageRepository ChatMessages { get; }
        public IPromotionRepository Promotions { get; }
        public IPromotionTypeRepository PromotionTypes { get; }
        public IGuardianRepository Guardians { get; }
        public ICamperGuardianRepository CamperGuardians { get; }
        public ICamperActivityRepository CamperActivities { get; }
        public ICamperTransportRepository CamperTransports { get; }
        public IHealthRecordRepository HealthRecords { get; }
        public ITransactionRepository Transactions { get; }
        public ITransportScheduleRepository TransportSchedules { get; }
        public ILocationRepository Locations { get; }
        public IGroupActivityRepository GroupActivities { get; }
        public IAttendanceLogRepository AttendanceLogs { get; }
        public ICamperAccomodationRepository CamperAccommodations { get; }
        public IRegistrationCamperRepository RegistrationCampers { get; }
        public IParentCamperRepository ParentCampers { get; }
        public IDriverRepository Drivers { get; }
        public ILiveStreamRepository LiveStreams { get; }
        public UnitOfWork(CampEaseDatabaseContext context, IUserRepository userRepository, 
            IRefreshTokenRepository refreshTokenRepository, IVehicleRepository vehicles,
            IVehicleTypeRepository vehicleTypes, ICampRepository campRepository, ICampTypeRepository campTypes
            , ICamperGroupRepository camperGroups, IRegistrationRepository registrations, ICamperRepository campers,
            IBlogRepository blogs, IRouteRepository routes, IPromotionTypeRepository promotionTypes,
            IGuardianRepository guardians, IActivityRepository activities, ICamperActivityRepository camperActivities,
            IHealthRecordRepository healthRecords, IPromotionRepository promotions, ITransactionRepository transactions
            , ILocationRepository locations, IRegistrationOptionalActivityRepository registrationOptionalActivities
            , IActivityScheduleRepository activitySchedules, IGroupActivityRepository groupActivities, IAlbumRepository albums, IAlbumPhotoRepository albumPhotos
            , IUserAccountRepository userAccounts, IAttendanceLogRepository attendanceLogs, IAlbumPhotoFaceRepository albumPhotoFaces,
            ICamperAccomodationRepository camperAccomodations, IRegistrationCamperRepository registrationCampers, ICampStaffAssignmentRepository campStaffAssignments
            , IChatConversationRepository chatConversations, IChatMessageRepository chatMessages, IParentCamperRepository parentCampers, IAccommodationRepository accommodations
            , IRouteStopRepository routeStops, IAccommodationTypeRepository accommodationTypes, ICamperGuardianRepository camperGuardians,
            ITransportScheduleRepository transportSchedules, IDriverRepository drivers, ILiveStreamRepository liveStreams
            , IReportRepository reports, ICamperTransportRepository camperTransport
            )
        {
            _context = context;
            Accommodations = accommodations;
            AccommodationTypes = accommodationTypes;
            Activities = activities;
            ActivitySchedules = activitySchedules;
            Albums = albums;
            AlbumPhotos = albumPhotos;
            AlbumPhotoFaces = albumPhotoFaces;
            AttendanceLogs = attendanceLogs;
            Blogs = blogs;
            Camps = campRepository;
            CampTypes = campTypes;
            CamperGroups = camperGroups;
            Campers = campers;
            CamperAccommodations = camperAccomodations;
            CamperGuardians = camperGuardians;
            CamperActivities = camperActivities;
            CampStaffAssignments = campStaffAssignments;
            CamperTransports = camperTransport;
            ChatConversations = chatConversations;
            ChatMessages = chatMessages;
            Drivers = drivers;
            Guardians = guardians;
            GroupActivities = groupActivities;
            HealthRecords = healthRecords;
            LiveStreams = liveStreams;
            Locations = locations;
            Promotions = promotions;
            PromotionTypes = promotionTypes;
            ParentCampers = parentCampers;
            Registrations = registrations;
            RegistrationOptionalActivities = registrationOptionalActivities;
            Routes = routes;
            RouteStops = routeStops;
            RegistrationCampers = registrationCampers;
            RefreshTokens = refreshTokenRepository;
            Transactions = transactions;
            TransportSchedules = transportSchedules;
            Users = userRepository;
            UserAccounts = userAccounts;
            Vehicles = vehicles;
            VehicleTypes = vehicleTypes;
            Reports = reports;
        }

        public async Task<int> CommitAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }

        public CampEaseDatabaseContext GetDbContext()
        {
            return _context;
        }
    }
}
