using AutoMapper;
using Moq;
using SummerCampManagementSystem.BLL.DTOs.Accommodation;
using SummerCampManagementSystem.BLL.DTOs.CampStaffAssignment;
using SummerCampManagementSystem.BLL.Helpers;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.BLL.Services;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;

namespace SummerCampManagementSystem.BLL.Tests.Services
{
    public class AccommodationServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IUserContextService> _mockUserContext;
        private readonly Mock<ICampStaffAssignmentService> _mockAssignmentService;
        private readonly AccommodationService _accommodationService;
        private readonly IConfigurationProvider _realMapperConfiguration;

        public AccommodationServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _mockUserContext = new Mock<IUserContextService>();
            _mockAssignmentService = new Mock<ICampStaffAssignmentService>();

            // --- setup automapper config ---
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Accommodation, AccommodationResponseDto>();
                cfg.CreateMap<AccommodationRequestDto, Accommodation>();
            });
            _realMapperConfiguration = config;
            _mockMapper.Setup(m => m.ConfigurationProvider).Returns(_realMapperConfiguration);

            _accommodationService = new AccommodationService(
                _mockUnitOfWork.Object,
                _mockMapper.Object,
                _mockUserContext.Object,
                _mockAssignmentService.Object
            );
        }

        // UT05 - TEST CASE 1: Supervisor not found
        [Fact]
        public async Task CreateAcc_SupervisorNotFound_ThrowsKeyNotFound()
        {
            // arrange
            var request = new AccommodationRequestDto { campId = 1, supervisorId = 99 };
            _mockUnitOfWork.Setup(u => u.Camps.GetByIdAsync(1)).ReturnsAsync(new Camp { status = "Draft" });
            _mockUnitOfWork.Setup(u => u.Users.GetByIdAsync(99)).ReturnsAsync((UserAccount?)null);

            // act & assert
            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => _accommodationService.CreateAccommodationAsync(request));
            Assert.Contains("Supervisor with ID 99 not found", ex.Message);
        }

        // UT05 - TEST CASE 2: Invalid Role (User)
        [Fact]
        public async Task CreateAcc_InvalidRole_ThrowsArgumentException()
        {
            // arrange
            var request = new AccommodationRequestDto { campId = 1, supervisorId = 1 };
            _mockUnitOfWork.Setup(u => u.Camps.GetByIdAsync(1)).ReturnsAsync(new Camp { status = "Draft" });
            _mockUnitOfWork.Setup(u => u.Users.GetByIdAsync(1))
                .ReturnsAsync(new UserAccount { role = "User" });

            // act & assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _accommodationService.CreateAccommodationAsync(request));
            Assert.Contains("not a Staff or Manager", ex.Message);
        }

        // UT05 - TEST CASE 3: Supervisor Conflict (Already supervises another acc)
        [Fact]
        public async Task CreateAcc_SupervisorConflict_ThrowsInvalidOperation()
        {
            // arrange
            var request = new AccommodationRequestDto { campId = 1, supervisorId = 1 };

            _mockUnitOfWork.Setup(u => u.Camps.GetByIdAsync(1)).ReturnsAsync(new Camp { status = "Draft" });
            _mockUnitOfWork.Setup(u => u.Users.GetByIdAsync(1)).ReturnsAsync(new UserAccount { role = "Staff" });

            // mock assignment check (already assigned to camp)
            _mockAssignmentService.Setup(s => s.IsStaffAssignedToCampAsync(1, 1)).ReturnsAsync(true);

            // mock existing accommodation for this supervisor
            _mockUnitOfWork.Setup(u => u.Accommodations.GetBySupervisorIdAsync(1, 1))
                .ReturnsAsync(new Accommodation { accommodationId = 100 });

            // act & assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _accommodationService.CreateAccommodationAsync(request));
            Assert.Contains("already supervising accommodation", ex.Message);
        }

        // UT05 - TEST CASE 4: Auto-assign staff to camp + Create Success
        [Fact]
        public async Task CreateAcc_StaffNotAssignedToCamp_AutoAssignsAndCreates()
        {
            // arrange
            var request = new AccommodationRequestDto { campId = 1, supervisorId = 1, name = "Room A" };

            _mockUnitOfWork.Setup(u => u.Camps.GetByIdAsync(1)).ReturnsAsync(new Camp { status = "Draft" });
            _mockUnitOfWork.Setup(u => u.Users.GetByIdAsync(1)).ReturnsAsync(new UserAccount { role = "Staff" });

            // mock: staff NOT assigned to camp yet
            _mockAssignmentService.Setup(s => s.IsStaffAssignedToCampAsync(1, 1)).ReturnsAsync(false);

            // mock: no conflict accommodation
            _mockUnitOfWork.Setup(u => u.Accommodations.GetBySupervisorIdAsync(1, 1)).ReturnsAsync((Accommodation?)null);

            // setup mapper
            _mockMapper.Setup(m => m.Map<Accommodation>(request)).Returns(new Accommodation { campId = 1, supervisorId = 1 });
            _mockMapper.Setup(m => m.Map<AccommodationResponseDto>(It.IsAny<Accommodation>()))
                .Returns(new AccommodationResponseDto { name = "Room A" });

            // act
            var result = await _accommodationService.CreateAccommodationAsync(request);

            // assert
            Assert.NotNull(result);

            // verify auto-assign was called
            _mockAssignmentService.Verify(s => s.AssignStaffToCampAsync(It.IsAny<CampStaffAssignmentRequestDto>()), Times.Once);

            // verify create accommodation
            _mockUnitOfWork.Verify(u => u.Accommodations.CreateAsync(It.IsAny<Accommodation>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        }

        // UT05 - TEST CASE 5: Normal Success (Already assigned to camp)
        [Fact]
        public async Task CreateAcc_ValidData_ReturnsSuccess()
        {
            // arrange
            var request = new AccommodationRequestDto { campId = 1, supervisorId = 1, name = "Room B" };

            _mockUnitOfWork.Setup(u => u.Camps.GetByIdAsync(1)).ReturnsAsync(new Camp { status = "Draft" });
            _mockUnitOfWork.Setup(u => u.Users.GetByIdAsync(1)).ReturnsAsync(new UserAccount { role = "Staff" });
            _mockAssignmentService.Setup(s => s.IsStaffAssignedToCampAsync(1, 1)).ReturnsAsync(true); // Already assigned
            _mockUnitOfWork.Setup(u => u.Accommodations.GetBySupervisorIdAsync(1, 1)).ReturnsAsync((Accommodation?)null);

            _mockMapper.Setup(m => m.Map<Accommodation>(request)).Returns(new Accommodation());
            _mockMapper.Setup(m => m.Map<AccommodationResponseDto>(It.IsAny<Accommodation>()))
                .Returns(new AccommodationResponseDto { name = "Room B" });

            // act
            var result = await _accommodationService.CreateAccommodationAsync(request);

            // assert
            Assert.NotNull(result);

            // verify auto-assign was NOT called
            _mockAssignmentService.Verify(s => s.AssignStaffToCampAsync(It.IsAny<CampStaffAssignmentRequestDto>()), Times.Never);

            _mockUnitOfWork.Verify(u => u.Accommodations.CreateAsync(It.IsAny<Accommodation>()), Times.Once);
        }
    }
}