using Microsoft.EntityFrameworkCore.Storage;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;

namespace SummerCampManagementSystem.DAL.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        IAccommodationRepository Accommodations { get; }
        IAccommodationTypeRepository AccommodationTypes { get; }
        IActivityRepository Activities { get; }
        IActivityScheduleRepository ActivitySchedules { get; }
        IAlbumRepository Albums { get; }
        IAlbumPhotoRepository AlbumPhotos { get; }
        IAlbumPhotoFaceRepository AlbumPhotoFaces { get; }
        IBlogRepository Blogs { get; }
        IBankUserRepository BankUsers { get; }
        IUserRepository Users { get; }
        IUserAccountRepository UserAccounts { get; }
        IRefreshTokenRepository RefreshTokens { get; }
        IRegistrationRepository Registrations { get; }
        IRegistrationOptionalActivityRepository RegistrationOptionalActivities { get; }
        IRouteRepository Routes { get; }
        IRouteStopRepository RouteStops { get; }
        IVehicleRepository Vehicles { get; }
        IVehicleTypeRepository VehicleTypes { get; }
        ICamperGroupRepository CamperGroups { get; }
        ICampRepository Camps { get; }
        ICampTypeRepository CampTypes { get; }
        ICamperRepository Campers { get; }
        IChatConversationRepository ChatConversations { get; }
        IChatMessageRepository ChatMessages { get; }
        IChatRoomRepository ChatRooms { get; }
        IChatRoomUserRepository ChatRoomUsers { get; }
        ICampStaffAssignmentRepository CampStaffAssignments { get; }
        IPromotionRepository Promotions { get; }
        IPromotionTypeRepository PromotionTypes { get; }
        IGuardianRepository Guardians { get; }
        IGroupRepository Groups { get; }
        ICamperGuardianRepository CamperGuardians { get; }
        ICamperActivityRepository CamperActivities { get; }
        ICamperTransportRepository CamperTransports { get; }
        IHealthRecordRepository HealthRecords { get; }
        ITransactionRepository Transactions { get; }
        ITransportScheduleRepository TransportSchedules { get; }
        ILocationRepository Locations { get; }
        IGroupActivityRepository GroupActivities { get; }
        IAttendanceLogRepository AttendanceLogs { get; }
        ICamperAccomodationRepository CamperAccommodations { get; }
        IRegistrationCamperRepository RegistrationCampers { get; }
        IParentCamperRepository ParentCampers { get; }
        IDriverRepository Drivers { get; }
        ILiveStreamRepository LiveStreams { get; }
        IReportRepository Reports { get; }
        IFeedbackRepository Feedbacks { get; }
        IMessageRepository Messages { get; }
        Task<int> CommitAsync();
        Task<IDbContextTransaction> BeginTransactionAsync();
        CampEaseDatabaseContext GetDbContext();
    }
}
