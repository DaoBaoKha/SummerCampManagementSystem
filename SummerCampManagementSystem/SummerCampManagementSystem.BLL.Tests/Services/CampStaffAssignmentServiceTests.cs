using AutoMapper;
using Moq;
using SummerCampManagementSystem.BLL.DTOs.Camp;
using SummerCampManagementSystem.BLL.DTOs.CampStaffAssignment;
using SummerCampManagementSystem.BLL.DTOs.UserAccount;
using SummerCampManagementSystem.BLL.Services;
using SummerCampManagementSystem.BLL.Tests.Helpers;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;

namespace SummerCampManagementSystem.BLL.Tests.Services
{
    public class CampStaffAssignmentServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMapper> _mockMapper;
        private readonly CampStaffAssignmentService _assignmentService;
        private readonly IConfigurationProvider _realMapperConfiguration;

        public CampStaffAssignmentServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();

            // setup real mapper configuration
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<CampStaffAssignment, CampStaffAssignmentResponseDto>();

                cfg.CreateMap<UserAccount, StaffSummaryDto>()
                   .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.firstName + " " + src.lastName));

                cfg.CreateMap<Camp, CampSummaryDto>();

                cfg.CreateMap<CampStaffAssignmentRequestDto, CampStaffAssignment>();
            });
            _realMapperConfiguration = config;

            // mock mapper configuration provider
            _mockMapper.Setup(m => m.ConfigurationProvider).Returns(_realMapperConfiguration);

            _assignmentService = new CampStaffAssignmentService(
                _mockUnitOfWork.Object,
                _mockMapper.Object
            );
        }

        // UT03 - TEST CASE 1: Staff not found
        [Fact]
        public async Task AssignStaff_StaffNotFound_ThrowsKeyNotFound()
        {
            // arrange
            var request = new CampStaffAssignmentRequestDto { StaffId = 99, CampId = 1 };

            // mock user not found
            _mockUnitOfWork.Setup(u => u.Users.GetByIdAsync(99))
                .ReturnsAsync((UserAccount?)null);

            // act & assert
            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => _assignmentService.AssignStaffToCampAsync(request));
            Assert.Contains("Staff with ID 99 not found", ex.Message);
        }

        // UT03 - TEST CASE 2: Invalid role (user is not staff or manager)
        [Fact]
        public async Task AssignStaff_InvalidRole_ThrowsArgumentException()
        {
            // arrange
            var request = new CampStaffAssignmentRequestDto { StaffId = 1, CampId = 1 };

            // mock user with invalid role
            var normalUser = new UserAccount { userId = 1, role = "User" };
            _mockUnitOfWork.Setup(u => u.Users.GetByIdAsync(1)).ReturnsAsync(normalUser);

            // act & assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _assignmentService.AssignStaffToCampAsync(request));
            Assert.Contains("not a Staff or Manager", ex.Message);
        }

        // UT03 - TEST CASE 3: Camp not found
        [Fact]
        public async Task AssignStaff_CampNotFound_ThrowsKeyNotFound()
        {
            // arrange
            var request = new CampStaffAssignmentRequestDto { StaffId = 1, CampId = 99 };

            var staffUser = new UserAccount { userId = 1, role = "Staff" };
            _mockUnitOfWork.Setup(u => u.Users.GetByIdAsync(1)).ReturnsAsync(staffUser);

            // mock camp not found
            _mockUnitOfWork.Setup(u => u.Camps.GetByIdAsync(99))
                .ReturnsAsync((Camp?)null);

            // act & assert
            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => _assignmentService.AssignStaffToCampAsync(request));
            Assert.Contains("Camp with ID 99 not found", ex.Message);
        }

        // UT03 - TEST CASE 4: Duplicate assignment
        [Fact]
        public async Task AssignStaff_DuplicateAssignment_ThrowsArgumentException()
        {
            // arrange
            var request = new CampStaffAssignmentRequestDto { StaffId = 1, CampId = 1 };

            // mock valid user & camp
            _mockUnitOfWork.Setup(u => u.Users.GetByIdAsync(1)).ReturnsAsync(new UserAccount { userId = 1, role = "Staff" });
            _mockUnitOfWork.Setup(u => u.Camps.GetByIdAsync(1)).ReturnsAsync(new Camp { campId = 1 });

            // mock db already has this assignment record
            var existingAssignments = new List<CampStaffAssignment>
            {
                new CampStaffAssignment { staffId = 1, campId = 1 }
            };
            var mockSet = MockDbSetHelper.GetQueryableMockDbSet(existingAssignments);
            _mockUnitOfWork.Setup(u => u.CampStaffAssignments.GetQueryable()).Returns(mockSet.Object);

            // act & assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _assignmentService.AssignStaffToCampAsync(request));
            Assert.Contains("already assigned to this camp", ex.Message);
        }

        // UT03 - TEST CASE 5: Successful assignment
        [Fact]
        public async Task AssignStaff_ValidData_ReturnsSuccess()
        {
            // arrange
            var request = new CampStaffAssignmentRequestDto { StaffId = 1, CampId = 1 };

            _mockUnitOfWork.Setup(u => u.Users.GetByIdAsync(1)).ReturnsAsync(new UserAccount { userId = 1, role = "Staff" });
            _mockUnitOfWork.Setup(u => u.Camps.GetByIdAsync(1)).ReturnsAsync(new Camp { campId = 1 });

            // mock empty db
            var emptyList = new List<CampStaffAssignment>();
            var mockSet = MockDbSetHelper.GetQueryableMockDbSet(emptyList);
            _mockUnitOfWork.Setup(u => u.CampStaffAssignments.GetQueryable()).Returns(mockSet.Object);

            // setup createasync to simulate adding to db
            _mockUnitOfWork.Setup(u => u.CampStaffAssignments.CreateAsync(It.IsAny<CampStaffAssignment>()))
                .Callback<CampStaffAssignment>(a => {
                    a.campStaffAssignmentId = 10;
                    // Fix: Populate nested objects for AutoMapper
                    a.staff = new UserAccount { firstName = "Test", lastName = "Staff" };
                    a.camp = new Camp { name = "Test Camp" };
                    emptyList.Add(a);
                })
                .Returns(Task.CompletedTask);

            // setup mapper basic map
            _mockMapper.Setup(m => m.Map<CampStaffAssignment>(request)).Returns(new CampStaffAssignment { staffId = 1, campId = 1 });

            // act
            var result = await _assignmentService.AssignStaffToCampAsync(request);

            // assert
            Assert.NotNull(result);
            Assert.Equal(10, result.CampStaffAssignmentId);

            _mockUnitOfWork.Verify(u => u.CampStaffAssignments.CreateAsync(It.IsAny<CampStaffAssignment>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        }
    }
}