using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using SummerCampManagementSystem.BLL.DTOs.Camper;
using SummerCampManagementSystem.BLL.DTOs.CamperGroup;
using SummerCampManagementSystem.BLL.DTOs.Group;
using SummerCampManagementSystem.BLL.Exceptions;
using SummerCampManagementSystem.BLL.Services;
using SummerCampManagementSystem.BLL.Tests.Helpers;
using SummerCampManagementSystem.Core.Enums;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;
using SummerCampManagementSystem.DAL.UnitOfWork;

namespace SummerCampManagementSystem.BLL.Tests.Services
{
    public class CamperGroupServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<CamperGroupService>> _mockLogger;
        private readonly CamperGroupService _camperGroupService;

        private readonly Mock<ICamperRepository> _mockCamperRepo;
        private readonly Mock<IGroupRepository> _mockGroupRepo;
        private readonly Mock<ICampRepository> _mockCampRepo;
        private readonly Mock<ICamperGroupRepository> _mockCamperGroupRepo;
        private readonly Mock<IRegistrationCamperRepository> _mockRegCamperRepo;

        public CamperGroupServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<CamperGroupService>>();

            _mockCamperRepo = new Mock<ICamperRepository>();
            _mockGroupRepo = new Mock<IGroupRepository>();
            _mockCampRepo = new Mock<ICampRepository>();
            _mockCamperGroupRepo = new Mock<ICamperGroupRepository>();
            _mockRegCamperRepo = new Mock<IRegistrationCamperRepository>();

            _mockUnitOfWork.Setup(u => u.Campers).Returns(_mockCamperRepo.Object);
            _mockUnitOfWork.Setup(u => u.Groups).Returns(_mockGroupRepo.Object);
            _mockUnitOfWork.Setup(u => u.Camps).Returns(_mockCampRepo.Object);
            _mockUnitOfWork.Setup(u => u.CamperGroups).Returns(_mockCamperGroupRepo.Object);
            _mockUnitOfWork.Setup(u => u.RegistrationCampers).Returns(_mockRegCamperRepo.Object);

            _camperGroupService = new CamperGroupService(
                _mockUnitOfWork.Object,
                _mockMapper.Object,
                _mockLogger.Object
            );
        }

        // ==========================================
        // UT21 - ASSIGN CAMPER TO GROUP (AGE BOUNDARIES)
        // ==========================================

        private (Camper, Group, Camp) SetupForAssign(int camperId, int groupId, int campId, 
            int age, int groupMin, int groupMax, 
            int currentSize = 0, int maxSize = 10)
        {
            // calculate DOB based on age
            var today = DateOnly.FromDateTime(DateTime.Now);
            var dob = today.AddYears(-age);

            var camper = new Camper 
            { 
                camperId = camperId, 
                camperName = "Test Camper", 
                dob = dob 
            };

            var camp = new Camp 
            { 
                campId = campId, 
                name = "Test Camp", 
                status = CampStatus.OpenForRegistration.ToString(),
                startDate = DateTime.Now.AddDays(10) 
            };
            
            var group = new Group 
            { 
                groupId = groupId, 
                campId = campId,
                groupName = "Test Group",
                minAge = groupMin,
                maxAge = groupMax,
                currentSize = currentSize,
                maxSize = maxSize,
                CamperGroups = new List<CamperGroup>()
            };
            // fill current list to match size
            for(int i=0; i<currentSize; i++) group.CamperGroups.Add(new CamperGroup());

            // Mock returns
            _mockCamperRepo.Setup(r => r.GetByIdAsync(camperId)).ReturnsAsync(camper);
            _mockGroupRepo.Setup(r => r.GetByIdWithCamperGroupsAndCampAsync(groupId)).ReturnsAsync(group);
            _mockCampRepo.Setup(r => r.GetByIdAsync(campId)).ReturnsAsync(camp);
            
            // Mock existing assignment (null)
            _mockCamperGroupRepo.Setup(r => r.GetByCamperAndGroupAsync(camperId, groupId))
                .ReturnsAsync((CamperGroup?)null);

            // Mock Registration
            var regCamper = new RegistrationCamper 
            { 
                camperId = camperId, 
                status = RegistrationCamperStatus.Confirmed.ToString(),
                registration = new Registration { campId = campId }
            };
            var regCamperList = new List<RegistrationCamper> { regCamper };
            var mockRegSet = MockDbSetHelper.GetQueryableMockDbSet(regCamperList);
            _mockRegCamperRepo.Setup(r => r.GetQueryable()).Returns(mockRegSet.Object);

            // Mock mapper
            _mockMapper.Setup(m => m.Map<CamperGroupResponseDto>(It.IsAny<CamperGroup>()))
                .Returns(new CamperGroupResponseDto 
                { 
                    camperName = new CamperNameDto { CamperId = camperId },
                    groupName = new GroupNameDto { GroupId = groupId }
                });

            return (camper, group, camp);
        }

        // UT21 - TEST CASE 1: Camper Too Young
        [Fact]
        public async Task AssignCamper_AgeBelowMin_ThrowsBusinessRuleException()
        {
            // arrange
            SetupForAssign(1, 1, 1, age: 6, groupMin: 7, groupMax: 8);
            var req = new CamperGroupRequestDto { camperId = 1, groupId = 1 };

            // act & assert
            var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _camperGroupService.CreateCamperGroupAsync(req));
            Assert.Contains("nằm ngoài phạm vi cho phép", ex.Message);
        }

        // UT21 - TEST CASE 2: Camper Too Old
        [Fact]
        public async Task AssignCamper_AgeAboveMax_ThrowsBusinessRuleException()
        {
            // arrange
            SetupForAssign(1, 1, 1, age: 9, groupMin: 7, groupMax: 8);
            var req = new CamperGroupRequestDto { camperId = 1, groupId = 1 };

            // act & assert
            var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _camperGroupService.CreateCamperGroupAsync(req));
            Assert.Contains("nằm ngoài phạm vi cho phép", ex.Message);
        }

        // UT21 - TEST CASE 3: Camper Age Within Boundary (Upper Edge + Days)
        [Fact]
        public async Task AssignCamper_AgeWithinBoundary_UpperEdge_ReturnsSuccess()
        {
            // arrange
            int camperId = 1; int groupId = 1; int campId = 1;

            var (camper, group, camp) = SetupForAssign(camperId, groupId, campId, age: 8, groupMin: 7, groupMax: 8);

            // Override DOB to be 8 years + 2 days
            var today = DateOnly.FromDateTime(DateTime.Now);
            var dob = today.AddYears(-8).AddDays(-2); 
            camper.dob = dob;

            _mockCamperGroupRepo.Setup(r => r.CreateAsync(It.IsAny<CamperGroup>())).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(1);
            _mockUnitOfWork.Setup(u => u.CamperGroups.GetByIdWithDetailsAsync(It.IsAny<int>()))
                .ReturnsAsync(new CamperGroup { camperId = 1, groupId = 1 });

            // act
            var result = await _camperGroupService.CreateCamperGroupAsync(new CamperGroupRequestDto { camperId = 1, groupId = 1 });

            // assert
            Assert.NotNull(result);
            Assert.Equal(1, result.camperName?.CamperId);
        }

        // UT21 - TEST CASE 4: Group Full
        [Fact]
        public async Task AssignCamper_GroupFull_ThrowsBusinessRuleException()
        {
            // arrange
            SetupForAssign(1, 1, 1, age: 7, groupMin: 7, groupMax: 8, currentSize: 10, maxSize: 10);
            var req = new CamperGroupRequestDto { camperId = 1, groupId = 1 };

            // act & assert
            var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _camperGroupService.CreateCamperGroupAsync(req));
            Assert.Contains("đã đầy", ex.Message);
        }
    }
}
