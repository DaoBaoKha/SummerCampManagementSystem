using AutoMapper;
using Moq;
using SummerCampManagementSystem.BLL.DTOs.TransportSchedule;
using SummerCampManagementSystem.BLL.Exceptions;
using SummerCampManagementSystem.BLL.Helpers;
using SummerCampManagementSystem.BLL.Services;
using SummerCampManagementSystem.BLL.Tests.Helpers;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;
using SummerCampManagementSystem.DAL.UnitOfWork;

namespace SummerCampManagementSystem.BLL.Tests.Services
{
    public class TransportScheduleServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IUserContextService> _mockUserContext;

        // Mock Repositories
        private readonly Mock<ITransportScheduleRepository> _mockTransportRepo;
        private readonly Mock<ITransportScheduleRepository> _mockDirectRepo;
        private readonly Mock<ICampRepository> _mockCampRepo;
        private readonly Mock<IRouteRepository> _mockRouteRepo;
        private readonly Mock<IDriverRepository> _mockDriverRepo;
        private readonly Mock<IVehicleRepository> _mockVehicleRepo;

        private readonly TransportScheduleService _service;
        private readonly IConfigurationProvider _realMapperConfiguration;

        public TransportScheduleServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _mockUserContext = new Mock<IUserContextService>();

            // initialize mock repositories
            _mockTransportRepo = new Mock<ITransportScheduleRepository>();
            _mockDirectRepo = new Mock<ITransportScheduleRepository>();
            _mockCampRepo = new Mock<ICampRepository>();
            _mockRouteRepo = new Mock<IRouteRepository>();
            _mockDriverRepo = new Mock<IDriverRepository>();
            _mockVehicleRepo = new Mock<IVehicleRepository>();

            _mockUnitOfWork.Setup(u => u.TransportSchedules).Returns(_mockTransportRepo.Object);
            _mockUnitOfWork.Setup(u => u.Camps).Returns(_mockCampRepo.Object);
            _mockUnitOfWork.Setup(u => u.Routes).Returns(_mockRouteRepo.Object);
            _mockUnitOfWork.Setup(u => u.Drivers).Returns(_mockDriverRepo.Object);
            _mockUnitOfWork.Setup(u => u.Vehicles).Returns(_mockVehicleRepo.Object);

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<TransportSchedule, TransportScheduleResponseDto>();
                cfg.CreateMap<TransportScheduleRequestDto, TransportSchedule>();
            });
            _realMapperConfiguration = config;
            _mockMapper.Setup(m => m.ConfigurationProvider).Returns(_realMapperConfiguration);

            _service = new TransportScheduleService(
                _mockUnitOfWork.Object,
                _mockMapper.Object,
                _mockUserContext.Object,
                _mockDirectRepo.Object
            );
        }

        private DateOnly GetFutureDate(int daysToAdd = 5)
            => DateOnly.FromDateTime(DateTime.UtcNow.AddDays(daysToAdd));

        // UT07 - TEST CASE 1: Invalid Time
        [Fact]
        public async Task CreateSchedule_InvalidTime_ThrowsBusinessRuleException()
        {
            var request = new TransportScheduleRequestDto
            {
                StartTime = new TimeOnly(10, 0),
                EndTime = new TimeOnly(09, 0)
            };

            var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.CreateScheduleAsync(request));
            Assert.Contains("Thời gian bắt đầu phải sớm hơn", ex.Message);
        }

        // UT07 - TEST CASE 2: PickUp Date > Camp Start
        [Fact]
        public async Task CreateSchedule_PickUpDateAfterCampStart_ThrowsBusinessRuleException()
        {
            var futureDate = GetFutureDate(10);
            var request = new TransportScheduleRequestDto
            {
                StartTime = new TimeOnly(7, 0),
                EndTime = new TimeOnly(8, 0),
                Date = futureDate,
                TransportType = "PickUp",
                CampId = 1
            };

            var camp = new Camp
            {
                campId = 1,
                startDate = DateTime.UtcNow.AddDays(5),
                endDate = DateTime.UtcNow.AddDays(25)
            };
            // Setup trên MockRepo thay vì UnitOfWork direct chain
            _mockCampRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(camp);

            var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.CreateScheduleAsync(request));
            Assert.Contains("phải diễn ra trước hoặc vào ngày bắt đầu", ex.Message);
        }

        // UT07 - TEST CASE 3: Camp Not Found
        [Fact]
        public async Task CreateSchedule_CampNotFound_ThrowsNotFoundException()
        {
            var request = new TransportScheduleRequestDto
            {
                StartTime = new TimeOnly(7, 0),
                EndTime = new TimeOnly(8, 0),
                Date = GetFutureDate(1),
                TransportType = "PickUp",
                CampId = 99
            };

            _mockCampRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Camp?)null);

            var ex = await Assert.ThrowsAsync<NotFoundException>(() => _service.CreateScheduleAsync(request));
            Assert.Contains("Không tìm thấy Camp ID 99", ex.Message);
        }

        // UT07 - TEST CASE 4: Route belongs to wrong Camp
        [Fact]
        public async Task CreateSchedule_RouteWrongCamp_ThrowsBusinessRuleException()
        {
            var request = new TransportScheduleRequestDto
            {
                StartTime = new TimeOnly(7, 0),
                EndTime = new TimeOnly(8, 0),
                Date = GetFutureDate(2),
                TransportType = "PickUp",
                CampId = 1,
                RouteId = 2
            };

            var camp = new Camp { campId = 1, startDate = DateTime.UtcNow.AddDays(5), endDate = DateTime.UtcNow.AddDays(15) };
            _mockCampRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(camp);

            var route = new Route { routeId = 2, campId = 2, isActive = true };
            _mockRouteRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(route);

            var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.CreateScheduleAsync(request));
            Assert.Contains("không thuộc về Trại", ex.Message);
        }

        // UT07 - TEST CASE 5: Driver Conflict
        [Fact]
        public async Task CreateSchedule_DriverConflict_ThrowsBusinessRuleException()
        {
            var testDate = GetFutureDate(3);
            var request = new TransportScheduleRequestDto
            {
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(10, 0),
                Date = testDate,
                TransportType = "PickUp",
                CampId = 1,
                RouteId = 1,
                DriverId = 1,
                VehicleId = 1
            };

            // setup entities valid
            var camp = new Camp { campId = 1, startDate = DateTime.UtcNow.AddDays(5), endDate = DateTime.UtcNow.AddDays(15) };
            _mockCampRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(camp);
            _mockRouteRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Route { campId = 1, isActive = true });
            _mockDriverRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Driver { status = "Approved" });
            _mockVehicleRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Vehicle { status = "Active" });

            // to make conflict, create existing schedule that overlaps
            var existingSchedules = new List<TransportSchedule>
            {
                new TransportSchedule
                {
                    transportScheduleId = 100,
                    driverId = 1,
                    date = testDate,
                    startTime = new TimeOnly(0, 0),   // start of day
                    endTime = new TimeOnly(23, 59),   // end of day
                    status = "NotYet"
                }
            };

            var mockSet = MockDbSetHelper.GetQueryableMockDbSet(existingSchedules);
            _mockTransportRepo.Setup(r => r.GetQueryable()).Returns(mockSet.Object);

            var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.CreateScheduleAsync(request));
            Assert.Contains("lịch trình chồng chéo", ex.Message);
        }

        // UT07 - TEST CASE 6: Success
        [Fact]
        public async Task CreateSchedule_ValidData_ReturnsSuccess()
        {
            var request = new TransportScheduleRequestDto
            {
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(10, 0),
                Date = GetFutureDate(2),
                TransportType = "PickUp",
                CampId = 1,
                RouteId = 1,
                DriverId = 1,
                VehicleId = 1
            };

            var camp = new Camp { campId = 1, startDate = DateTime.UtcNow.AddDays(5), endDate = DateTime.UtcNow.AddDays(15) };
            _mockCampRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(camp);
            _mockRouteRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Route { campId = 1, isActive = true });
            _mockDriverRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Driver { status = "Approved" });
            _mockVehicleRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Vehicle { status = "Active" });

            // Mock DB empty
            var emptyList = new List<TransportSchedule>();
            var mockSet = MockDbSetHelper.GetQueryableMockDbSet(emptyList);

            _mockTransportRepo.Setup(r => r.GetQueryable()).Returns(mockSet.Object);
            _mockTransportRepo.Setup(r => r.CreateAsync(It.IsAny<TransportSchedule>()))
                .Callback<TransportSchedule>(s => {
                    s.transportScheduleId = 10;
                    emptyList.Add(s);
                })
                .Returns(Task.CompletedTask);

            // Setup Direct Repo
            var mockRepoSet = MockDbSetHelper.GetQueryableMockDbSet(emptyList);
            _mockDirectRepo.Setup(r => r.GetSchedulesWithIncludes()).Returns(mockRepoSet.Object);

            _mockMapper.Setup(m => m.Map<TransportSchedule>(request)).Returns(new TransportSchedule { campId = 1 });
            _mockMapper.Setup(m => m.Map<TransportScheduleResponseDto>(It.IsAny<TransportSchedule>()))
                .Returns(new TransportScheduleResponseDto { TransportScheduleId = 10 });

            var result = await _service.CreateScheduleAsync(request);

            Assert.NotNull(result);
            Assert.Equal(10, result.TransportScheduleId);

            _mockTransportRepo.Verify(r => r.CreateAsync(It.IsAny<TransportSchedule>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        }
    }
}