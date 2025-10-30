﻿using Microsoft.EntityFrameworkCore.Storage;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;

namespace SummerCampManagementSystem.DAL.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        IActivityRepository Activities { get; }
        IActivityScheduleRepository ActivitySchedules { get; }
        IAlbumRepository Albums { get; }
        IAlbumPhotoRepository AlbumPhotos { get; }
        IBlogRepository Blogs { get; }
        IUserRepository Users { get; }
        IUserAccountRepository UserAccounts { get; }
        IRefreshTokenRepository RefreshTokens { get; }
        IRegistrationRepository Registrations { get; }
        IRegistrationOptionalActivityRepository RegistrationOptionalActivities { get; }
        IRouteRepository Routes { get; }
        IVehicleRepository Vehicles { get; }
        IVehicleTypeRepository VehicleTypes { get; }
        ICamperGroupRepository CamperGroups { get; }
        ICampRepository Camps { get; }
        ICampTypeRepository CampTypes { get; }
        ICamperRepository Campers { get; }
        IPromotionRepository Promotions { get; }
        IPromotionTypeRepository PromotionTypes { get; }
        IGuardianRepository Guardians { get; }
        ICamperActivityRepository CamperActivities { get; }
        IHealthRecordRepository HealthRecords { get; }
        ITransactionRepository Transactions { get; }
        ILocationRepository Locations { get; }
        IGroupActivityRepository GroupActivities { get; }
        IAttendanceLogRepository AttendanceLogs { get; }
        Task<int> CommitAsync();
        Task<IDbContextTransaction> BeginTransactionAsync();
        CampEaseDatabaseContext GetDbContext();
    }
}
