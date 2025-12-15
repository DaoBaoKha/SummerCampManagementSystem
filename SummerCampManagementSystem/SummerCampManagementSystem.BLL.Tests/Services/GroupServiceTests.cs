using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Moq;
using SummerCampManagementSystem.BLL.DTOs.Group;
using SummerCampManagementSystem.BLL.Helpers;
using SummerCampManagementSystem.BLL.Services;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace SummerCampManagementSystem.BLL.Tests.Services
{
    public class GroupServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IValidationService> _mockValidationService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly GroupService _groupService;

        public GroupServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockValidationService = new Mock<IValidationService>();
            _mockMapper = new Mock<IMapper>();

            _groupService = new GroupService(
                _mockUnitOfWork.Object,
                _mockValidationService.Object,
                _mockMapper.Object
            );
        }

        // UT04 - TEST CASE 1: Camp not found (with supervisor)
        [Fact]
        public async Task CreateGroup_CampNotFound_WithSupervisor_ThrowsKeyNotFound()
        {
            // arrange
            var request = new GroupRequestDto { CampId = 99, SupervisorId = 1 };

            _mockUnitOfWork.Setup(u => u.Camps.GetByIdAsync(99))
                .ReturnsAsync((Camp?)null);

            // act & assert
            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => _groupService.CreateGroupAsync(request));
            Assert.Contains("Camp with ID 99 not found", ex.Message);
        }

        // UT04 - TEST CASE 2: Camp not found (no supervisor)
        [Fact]
        public async Task CreateGroup_CampNotFound_NoSupervisor_ThrowsKeyNotFound()
        {
            // arrange
            var request = new GroupRequestDto { CampId = 99, SupervisorId = null };

            // mock camp check directly
            _mockUnitOfWork.Setup(u => u.Camps.GetByIdAsync(99))
                .ReturnsAsync((Camp?)null);

            // act & assert
            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => _groupService.CreateGroupAsync(request));
            Assert.Contains("Camp with ID 99 not found", ex.Message);
        }

        // UT04 - TEST CASE 3: Supervisor not found
        [Fact]
        public async Task CreateGroup_SupervisorNotFound_ThrowsKeyNotFound()
        {
            // arrange
            var request = new GroupRequestDto { CampId = 1, SupervisorId = 99 };

            _mockUnitOfWork.Setup(u => u.Camps.GetByIdAsync(1)).ReturnsAsync(new Camp());
            _mockUnitOfWork.Setup(u => u.Users.GetByIdAsync(99)).ReturnsAsync((UserAccount?)null);

            // act & assert
            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => _groupService.CreateGroupAsync(request));
            Assert.Contains("Supervisor with ID 99 not found", ex.Message);
        }

        // UT04 - TEST CASE 4: Invalid supervisor role
        [Fact]
        public async Task CreateGroup_InvalidSupervisorRole_ThrowsArgumentException()
        {
            // arrange
            var request = new GroupRequestDto { CampId = 1, SupervisorId = 1 };

            _mockUnitOfWork.Setup(u => u.Camps.GetByIdAsync(1)).ReturnsAsync(new Camp());
            _mockUnitOfWork.Setup(u => u.Users.GetByIdAsync(1))
                .ReturnsAsync(new UserAccount { role = "User" });

            // act & assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _groupService.CreateGroupAsync(request));
            Assert.Contains("not a Staff member", ex.Message);
        }

        // UT04 - TEST CASE 5: Supervisor assignment conflict
        [Fact]
        public async Task CreateGroup_SupervisorAlreadyAssigned_ThrowsInvalidOperation()
        {
            // arrange
            var request = new GroupRequestDto { CampId = 1, SupervisorId = 1 };

            _mockUnitOfWork.Setup(u => u.Camps.GetByIdAsync(1)).ReturnsAsync(new Camp());
            _mockUnitOfWork.Setup(u => u.Users.GetByIdAsync(1))
                .ReturnsAsync(new UserAccount { role = "Staff" });

            // mock existing group assignment
            _mockUnitOfWork.Setup(u => u.Groups.GetGroupBySupervisorIdAsync(1, 1))
                .ReturnsAsync(new Group { groupId = 100 });

            // act & assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _groupService.CreateGroupAsync(request));
            Assert.Contains("already assigned to Camper Group", ex.Message);
        }

        // UT04 - TEST CASE 6: Successful creation
        [Fact]
        public async Task CreateGroup_ValidData_ReturnsSuccess()
        {
            // arrange
            var request = new GroupRequestDto { CampId = 1, SupervisorId = 1, GroupName = "Group A" };

            // validation mocks
            _mockUnitOfWork.Setup(u => u.Camps.GetByIdAsync(1)).ReturnsAsync(new Camp { campId = 1 });
            _mockUnitOfWork.Setup(u => u.Users.GetByIdAsync(1)).ReturnsAsync(new UserAccount { role = "Staff" });
            _mockUnitOfWork.Setup(u => u.Groups.GetGroupBySupervisorIdAsync(1, 1)).ReturnsAsync((Group?)null);

            // mock core schedules
            var coreSchedules = new List<ActivitySchedule>
            {
                new ActivitySchedule { activityScheduleId = 10 },
                new ActivitySchedule { activityScheduleId = 11 }
            };
            _mockUnitOfWork.Setup(u => u.ActivitySchedules.GetCoreScheduleByCampIdAsync(1))
                .ReturnsAsync(coreSchedules);

            // setup group creation
            _mockUnitOfWork.Setup(u => u.Groups.CreateAsync(It.IsAny<Group>()))
                .Callback<Group>(g => g.groupId = 5)
                .Returns(Task.CompletedTask);

            // setup group activity creation
            _mockUnitOfWork.Setup(u => u.GroupActivities.CreateAsync(It.IsAny<GroupActivity>()))
                .Returns(Task.CompletedTask);

            // setup mapper
            _mockMapper.Setup(m => m.Map<Group>(request)).Returns(new Group { campId = 1, supervisorId = 1 });
            _mockMapper.Setup(m => m.Map<GroupResponseDto>(It.IsAny<Group>()))
                .Returns(new GroupResponseDto { GroupId = 5, GroupName = "Group A" });

            // act
            var result = await _groupService.CreateGroupAsync(request);

            // assert
            Assert.NotNull(result);
            Assert.Equal(5, result.GroupId);

            _mockUnitOfWork.Verify(u => u.Groups.CreateAsync(It.IsAny<Group>()), Times.Once);
            // Verify core activities are copied (2 times)
            _mockUnitOfWork.Verify(u => u.GroupActivities.CreateAsync(It.IsAny<GroupActivity>()), Times.Exactly(2));
            _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Exactly(2));
        }
    }
}