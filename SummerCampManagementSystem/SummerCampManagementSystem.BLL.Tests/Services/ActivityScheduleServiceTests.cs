using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using SummerCampManagementSystem.BLL.DTOs.ActivitySchedule;
using SummerCampManagementSystem.BLL.Services;
using SummerCampManagementSystem.Core.Enums;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;
using SummerCampManagementSystem.DAL.UnitOfWork;

namespace SummerCampManagementSystem.BLL.Tests.Services
{
    public class ActivityScheduleServiceTests : IDisposable
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMapper> _mockMapper;
        private readonly ActivityScheduleService _service;
        private readonly CampEaseDatabaseContext _dbContext; // Use real context with InMemory

        // repositories (using mocks for others effectively, but DbContext is needed for Include)
        private readonly Mock<IActivityScheduleRepository> _mockScheduleRepo;
        private readonly Mock<IActivityRepository> _mockActivityRepo;
        private readonly Mock<ICampRepository> _mockCampRepo;
        private readonly Mock<ILocationRepository> _mockLocationRepo;
        private readonly Mock<IUserRepository> _mockUserRepo;
        private readonly Mock<IGroupRepository> _mockGroupRepo;
        private readonly Mock<IAccommodationRepository> _mockAccomRepo;
        private readonly Mock<IGroupActivityRepository> _mockGroupActivityRepo;
        private readonly Mock<IAccommodationActivityRepository> _mockAccomActivityRepo;

        public ActivityScheduleServiceTests()
        {
            // Setup InMemory Database
            var options = new DbContextOptionsBuilder<CampEaseDatabaseContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _dbContext = new CampEaseDatabaseContext(options);
            _dbContext.Database.EnsureCreated(); // Clean start

            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();

            // init mock repos
            _mockScheduleRepo = new Mock<IActivityScheduleRepository>();
            _mockActivityRepo = new Mock<IActivityRepository>();
            _mockCampRepo = new Mock<ICampRepository>();
            _mockLocationRepo = new Mock<ILocationRepository>();
            _mockUserRepo = new Mock<IUserRepository>();
            _mockGroupRepo = new Mock<IGroupRepository>();
            _mockAccomRepo = new Mock<IAccommodationRepository>();
            _mockGroupActivityRepo = new Mock<IGroupActivityRepository>();
            _mockAccomActivityRepo = new Mock<IAccommodationActivityRepository>();

            // setup unitofwork
            // Important: We need to ensure UnitOfWork returns our mocks for Repos, but the InMemory DbContext for GetDbContext()
            _mockUnitOfWork.Setup(u => u.ActivitySchedules).Returns(_mockScheduleRepo.Object);
            _mockUnitOfWork.Setup(u => u.Activities).Returns(_mockActivityRepo.Object);
            _mockUnitOfWork.Setup(u => u.Camps).Returns(_mockCampRepo.Object);
            _mockUnitOfWork.Setup(u => u.Locations).Returns(_mockLocationRepo.Object);
            _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepo.Object);
            _mockUnitOfWork.Setup(u => u.Groups).Returns(_mockGroupRepo.Object);
            _mockUnitOfWork.Setup(u => u.Accommodations).Returns(_mockAccomRepo.Object);
            _mockUnitOfWork.Setup(u => u.GroupActivities).Returns(_mockGroupActivityRepo.Object);
            _mockUnitOfWork.Setup(u => u.AccommodationActivities).Returns(_mockAccomActivityRepo.Object);
            
            // Return the InMemory context
            _mockUnitOfWork.Setup(u => u.GetDbContext()).Returns(_dbContext);

            // mock transaction
            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync())
                .ReturnsAsync(new Mock<IDbContextTransaction>().Object);

            _service = new ActivityScheduleService(_mockUnitOfWork.Object, _mockMapper.Object);
        }

        public void Dispose()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }

        // UT08 - TEST CASE 1: Invalid Time (Start >= End)
        [Fact]
        public async Task CreateCore_InvalidTime_ThrowsInvalidOperation()
        {
            var activity = new Activity { activityId = 1, campId = 1, activityType = "Core" }; 
            _mockActivityRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(activity);
            _mockCampRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Camp());

            var dto = new ActivityScheduleCreateDto
            {
                ActivityId = 1,
                StartTime = DateTime.UtcNow.AddHours(2),
                EndTime = DateTime.UtcNow.AddHours(1) // Error
            };

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateCoreScheduleAsync(dto));
            Assert.Contains("Giờ bắt đầu phải sớm hơn giờ kết thúc", ex.Message);
        }

        // UT08 - TEST CASE 2: Outside Camp Duration
        [Fact]
        public async Task CreateCore_OutsideCampDuration_ReturnsError()
        {
            var activity = new Activity { activityId = 1, campId = 1, activityType = "Core" };
            var camp = new Camp
            {
                campId = 1,
                startDate = DateTime.UtcNow.AddDays(5),
                endDate = DateTime.UtcNow.AddDays(10)
            };

            _mockActivityRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(activity);
            _mockCampRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(camp);

            var dto = new ActivityScheduleCreateDto
            {
                ActivityId = 1,
                StartTime = DateTime.UtcNow.AddDays(1), // Before camp start
                EndTime = DateTime.UtcNow.AddDays(2)
            };

            var result = await _service.CreateCoreScheduleAsync(dto);
            Assert.Contains(result.Errors, e => e.Contains("Nằm ngoài thời gian trại"));
        }

        // UT08 - TEST CASE 3: Overlap with another Core Activity
        [Fact]
        public async Task CreateCore_TimeOverlap_ReturnsError()
        {
            var activity = new Activity { activityId = 1, campId = 1, activityType = "Core" };
            var camp = new Camp
            {
                campId = 1,
                startDate = DateTime.UtcNow.AddDays(1),
                endDate = DateTime.UtcNow.AddDays(5)
            };

            _mockActivityRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(activity);
            _mockCampRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(camp);

            // Populate InMemory DB to trigger overlap
            var existingSchedule = new ActivitySchedule
            {
                // activityScheduleId is auto-generated or can be set, EF handles it
                startTime = DateTime.UtcNow.AddDays(2),
                endTime = DateTime.UtcNow.AddDays(3),
                activity = new Activity { activityId = 2, campId = 1, activityType = ActivityType.Optional.ToString() }, // Conflict: Optional overlaps
                activityId = 2
            };
            
            _dbContext.ActivitySchedules.Add(existingSchedule);
            await _dbContext.SaveChangesAsync();

            var dto = new ActivityScheduleCreateDto
            {
                ActivityId = 1,
                StartTime = DateTime.UtcNow.AddDays(2).AddMinutes(1),
                EndTime = DateTime.UtcNow.AddDays(2).AddMinutes(60)
            };

            // Service logic returns error in result.Errors
            var result = await _service.CreateCoreScheduleAsync(dto);
            Assert.Contains(result.Errors, e => e.Contains("Bị trùng lịch với hoạt động Optional/Resting"));
        }

        // UT08 - TEST CASE 4: Location Conflict
        [Fact]
        public async Task CreateCore_LocationConflict_ReturnsError()
        {
            var activity = new Activity { activityId = 1, campId = 1, activityType = "Core" };
            var camp = new Camp { campId = 1, startDate = DateTime.UtcNow.AddDays(1), endDate = DateTime.UtcNow.AddDays(5) };

            _mockActivityRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(activity);
            _mockCampRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(camp);
            _mockLocationRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Location());

            // Mock call result on Repo because logic uses Repo method
            _mockScheduleRepo.Setup(r => r.ExistsInSameTimeAndLocationAsync(1, It.IsAny<DateTime>(), It.IsAny<DateTime>(), null))
                .ReturnsAsync(true);

            var dto = new ActivityScheduleCreateDto
            {
                ActivityId = 1,
                LocationId = 1,
                StartTime = DateTime.UtcNow.AddDays(2),
                EndTime = DateTime.UtcNow.AddDays(3)
            };

            var result = await _service.CreateCoreScheduleAsync(dto);
            Assert.Contains(result.Errors, e => e.Contains("Địa điểm đã có hoạt động khác"));
        }

        // UT08 - TEST CASE 5: Staff Busy
        [Fact]
        public async Task CreateCore_StaffBusy_ReturnsError()
        {
            var activity = new Activity { activityId = 1, campId = 1, activityType = "Core" };
            var camp = new Camp { campId = 1, startDate = DateTime.UtcNow.AddDays(1), endDate = DateTime.UtcNow.AddDays(5) };

            _mockActivityRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(activity);
            _mockCampRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(camp);
            _mockUserRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new UserAccount { role = "Staff" });

            // Service checks staff busy via Repo
            _mockScheduleRepo.Setup(r => r.IsStaffBusyAsync(1, It.IsAny<DateTime>(), It.IsAny<DateTime>(), null))
                .ReturnsAsync(true);

            var dto = new ActivityScheduleCreateDto
            {
                ActivityId = 1,
                StaffId = 1,
                StartTime = DateTime.UtcNow.AddDays(2),
                EndTime = DateTime.UtcNow.AddDays(3)
            };

            var result = await _service.CreateCoreScheduleAsync(dto);
            Assert.Contains(result.Errors, e => e.Contains("Staff bận"));
        }

        // UT08 - TEST CASE 6: Invalid Staff Role
        [Fact]
        public async Task CreateCore_InvalidStaffRole_ThrowsInvalidOperation()
        {
            var activity = new Activity { activityId = 1, campId = 1, activityType = "Core" };
            var camp = new Camp { campId = 1, startDate = DateTime.UtcNow.AddDays(1), endDate = DateTime.UtcNow.AddDays(5) };

            _mockActivityRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(activity);
            _mockCampRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(camp);

            // mock user is not staff
            _mockUserRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new UserAccount { role = "User" });

            var dto = new ActivityScheduleCreateDto
            {
                ActivityId = 1,
                StaffId = 1,
                StartTime = DateTime.UtcNow.AddDays(2),
                EndTime = DateTime.UtcNow.AddDays(3)
            };

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateCoreScheduleAsync(dto));
            Assert.Contains("User được gán không phải là Staff", ex.Message);
        }

        // UT08 - TEST CASE 7: Livestream missing Staff
        [Fact]
        public async Task CreateCore_LivestreamNoStaff_DoesNotThrow()
        {
            var activity = new Activity { activityId = 1, campId = 1, activityType = "Core" };
            var camp = new Camp { campId = 1, startDate = DateTime.UtcNow.AddDays(1), endDate = DateTime.UtcNow.AddDays(5) };

            _mockActivityRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(activity);
            _mockCampRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(camp);

            // Mock CreateAsync
            _mockScheduleRepo.Setup(r => r.CreateAsync(It.IsAny<ActivitySchedule>())).Returns(Task.CompletedTask);
            var createdSchedule = new ActivitySchedule { activity = activity };
            _mockScheduleRepo.Setup(r => r.GetByIdWithActivityAsync(It.IsAny<int>())).ReturnsAsync(createdSchedule);
             _mockMapper.Setup(m => m.Map<ActivitySchedule>(It.IsAny<ActivityScheduleCreateDto>())).Returns(new ActivitySchedule());
             _mockMapper.Setup(m => m.Map<ActivityScheduleResponseDto>(It.IsAny<ActivitySchedule>())).Returns(new ActivityScheduleResponseDto());

            var dto = new ActivityScheduleCreateDto
            {
                ActivityId = 1,
                IsLiveStream = true,
                StaffId = null, 
                StartTime = DateTime.UtcNow.AddDays(2),
                EndTime = DateTime.UtcNow.AddDays(3)
            };

            // Should not throw
            var result = await _service.CreateCoreScheduleAsync(dto);
            Assert.NotNull(result);
        }

        // UT08 - TEST CASE 8: Success
        [Fact]
        public async Task CreateCore_ValidData_ReturnsSuccess()
        {
            var activity = new Activity { activityId = 1, campId = 1, activityType = ActivityType.Core.ToString() };
            var camp = new Camp { campId = 1, startDate = DateTime.UtcNow.AddDays(1), endDate = DateTime.UtcNow.AddDays(5) };

            _mockActivityRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(activity);
            _mockCampRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(camp);

            _mockAccomRepo.Setup(r => r.GetByCampId(1)).ReturnsAsync(new List<Accommodation>());

            // mock create
            _mockScheduleRepo.Setup(r => r.CreateAsync(It.IsAny<ActivitySchedule>()))
                .Callback<ActivitySchedule>(s => s.activityScheduleId = 100)
                .Returns(Task.CompletedTask);

            // mock retrieve result
            var createdSchedule = new ActivitySchedule
            {
                activityScheduleId = 100,
                activity = activity
            };
            _mockScheduleRepo.Setup(r => r.GetByIdWithActivityAsync(100)).ReturnsAsync(createdSchedule);

            _mockMapper.Setup(m => m.Map<ActivitySchedule>(It.IsAny<ActivityScheduleCreateDto>()))
                .Returns(new ActivitySchedule());
            _mockMapper.Setup(m => m.Map<ActivityScheduleResponseDto>(createdSchedule))
                .Returns(new ActivityScheduleResponseDto { ActivityScheduleId = 100 });

            var dto = new ActivityScheduleCreateDto
            {
                ActivityId = 1,
                StartTime = DateTime.UtcNow.AddDays(2),
                EndTime = DateTime.UtcNow.AddDays(3)
            };

            var result = await _service.CreateCoreScheduleAsync(dto);

            Assert.NotNull(result);
            Assert.NotNull(result.Successes);
            Assert.Single(result.Successes);
            Assert.Equal(100, result.Successes[0].ActivityScheduleId);

            _mockScheduleRepo.Verify(r => r.CreateAsync(It.IsAny<ActivitySchedule>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.AtLeastOnce);
        }
    }
}