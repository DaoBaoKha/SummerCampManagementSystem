using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using SummerCampManagementSystem.BLL.DTOs.Camp;
using SummerCampManagementSystem.BLL.Exceptions;
using SummerCampManagementSystem.BLL.Helpers;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.BLL.Services;
using SummerCampManagementSystem.BLL.Tests.Helpers;
using SummerCampManagementSystem.Core.Enums;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;
using SummerCampManagementSystem.DAL.UnitOfWork;

namespace SummerCampManagementSystem.BLL.Tests.Services
{
    public class CampServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IUserContextService> _mockUserContext;
        private readonly Mock<ILogger<CampService>> _mockLogger;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<IRefundService> _mockRefundService;
        private readonly CampService _campService;

        // mock repositories
        private readonly Mock<ICampRepository> _mockCampRepo;
        private readonly Mock<IActivityRepository> _mockActivityRepo;

        // setup for approved camp tests
        private readonly Mock<IActivityScheduleRepository> _mockActScheduleRepo;
        private readonly Mock<ITransportScheduleRepository> _mockTransScheduleRepo;
        private readonly Mock<IRouteRepository> _mockRouteRepo;

        public CampServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _mockUserContext = new Mock<IUserContextService>();
            _mockLogger = new Mock<ILogger<CampService>>();
            _mockEmailService = new Mock<IEmailService>();
            _mockRefundService = new Mock<IRefundService>();

            // init sub-repos
            _mockCampRepo = new Mock<ICampRepository>();
            _mockActivityRepo = new Mock<IActivityRepository>();
            _mockActScheduleRepo = new Mock<IActivityScheduleRepository>();
            _mockTransScheduleRepo = new Mock<ITransportScheduleRepository>();
            _mockRouteRepo = new Mock<IRouteRepository>();

            // setup unitofwork
            _mockUnitOfWork.Setup(u => u.Camps).Returns(_mockCampRepo.Object);
            _mockUnitOfWork.Setup(u => u.Activities).Returns(_mockActivityRepo.Object);
            _mockUnitOfWork.Setup(u => u.ActivitySchedules).Returns(_mockActScheduleRepo.Object);
            _mockUnitOfWork.Setup(u => u.TransportSchedules).Returns(_mockTransScheduleRepo.Object);
            _mockUnitOfWork.Setup(u => u.Routes).Returns(_mockRouteRepo.Object);

            _campService = new CampService(
                _mockUnitOfWork.Object,
                _mockMapper.Object,
                _mockUserContext.Object,
                _mockLogger.Object,
                _mockEmailService.Object,
                _mockRefundService.Object
            );
        }

        // ==========================================
        // UT02 - CREATE CAMP
        // ==========================================

        // UT02 - TEST CASE 1: Fail to create (registration end date is after start date)
        [Fact]
        public async Task CreateCamp_RegEndAfterStart_ThrowsBadRequest()
        {
            // arrange
            var request = new CampRequestDto
            {
                Name = "Invalid Date",
                StartDate = DateTime.UtcNow.AddDays(20),
                RegistrationEndDate = DateTime.UtcNow.AddDays(21), // error: end > start
                EndDate = DateTime.UtcNow.AddDays(25),
                RegistrationStartDate = DateTime.UtcNow,
                LocationId = 1,
                CampTypeId = 1,
                MinParticipants = 10,
                MaxParticipants = 20,
                MinAge = 10,
                MaxAge = 15,
                Description = "Desc",
                Place = "Place",
                Address = "Addr"
            };

            // act & assert
            var ex = await Assert.ThrowsAsync<BadRequestException>(() => _campService.CreateCampAsync(request));
            Assert.Contains("Ngày đóng đăng ký phải trước ngày bắt đầu", ex.Message);
        }

        // UT02 - TEST CASE 2: Fail to create (buffer time less than 10 days)
        [Fact]
        public async Task CreateCamp_BufferTimeTooShort_ThrowsBadRequest()
        {
            // arrange
            var request = new CampRequestDto
            {
                Name = "Short Buffer",
                RegistrationStartDate = DateTime.UtcNow,
                RegistrationEndDate = DateTime.UtcNow.AddDays(5),
                StartDate = DateTime.UtcNow.AddDays(10), // error: only 5 days diff
                EndDate = DateTime.UtcNow.AddDays(15),
                LocationId = 1,
                CampTypeId = 1,
                MinParticipants = 10,
                MaxParticipants = 20,
                MinAge = 10,
                MaxAge = 15,
                Description = "Desc",
                Place = "Place",
                Address = "Addr"
            };

            // act & assert
            var ex = await Assert.ThrowsAsync<BadRequestException>(() => _campService.CreateCampAsync(request));
            Assert.Contains("Ngày đóng đăng ký phải trước ngày bắt đầu ít nhất 10 ngày", ex.Message);
        }

        // UT02 - TEST CASE 3: Fail to create (duration less than 3 days)
        [Fact]
        public async Task CreateCamp_DurationTooShort_ThrowsBadRequest()
        {
            // arrange
            var request = new CampRequestDto
            {
                Name = "Short Duration",
                RegistrationStartDate = DateTime.UtcNow,
                RegistrationEndDate = DateTime.UtcNow.AddDays(10),
                StartDate = DateTime.UtcNow.AddDays(25),
                EndDate = DateTime.UtcNow.AddDays(26), // error: 1 day duration
                LocationId = 1,
                CampTypeId = 1,
                MinParticipants = 10,
                MaxParticipants = 20,
                MinAge = 10,
                MaxAge = 15,
                Description = "Desc",
                Place = "Place",
                Address = "Addr"
            };

            // act & assert
            var ex = await Assert.ThrowsAsync<BadRequestException>(() => _campService.CreateCampAsync(request));
            Assert.Contains("Thời lượng trại phải kéo dài ít nhất 3 ngày", ex.Message);
        }

        // UT02 - TEST CASE 4: Fail to create (location overlap)
        [Fact]
        public async Task CreateCamp_LocationOverlap_ThrowsBadRequest()
        {
            // arrange
            var request = new CampRequestDto
            {
                Name = "Overlap",
                RegistrationStartDate = DateTime.UtcNow,
                RegistrationEndDate = DateTime.UtcNow.AddDays(10),
                StartDate = DateTime.UtcNow.AddDays(25),
                EndDate = DateTime.UtcNow.AddDays(30),
                LocationId = 99,
                CampTypeId = 1,
                MinParticipants = 10,
                MaxParticipants = 20,
                MinAge = 10,
                MaxAge = 15,
                Description = "Desc",
                Place = "Place",
                Address = "Addr"
            };

            // mock existing camp in db
            var existingCamps = new List<Camp>
            {
                new Camp {
                    campId = 1, locationId = 99, status = "Published",
                    startDate = DateTime.UtcNow.AddDays(24), endDate = DateTime.UtcNow.AddDays(31)
                }
            };
            var mockSet = MockDbSetHelper.GetQueryableMockDbSet(existingCamps);
            _mockCampRepo.Setup(r => r.GetQueryable()).Returns(mockSet.Object);

            // act & assert
            var ex = await Assert.ThrowsAsync<BadRequestException>(() => _campService.CreateCampAsync(request));
            Assert.Contains("Địa điểm này đã có Camp", ex.Message);
        }

        // UT02 - TEST CASE 5: Successful creation (happy path)
        [Fact]
        public async Task CreateCamp_ValidData_ReturnsResponse()
        {
            // arrange
            var request = new CampRequestDto
            {
                Name = "Valid Camp",
                RegistrationStartDate = DateTime.UtcNow,
                RegistrationEndDate = DateTime.UtcNow.AddDays(10),
                StartDate = DateTime.UtcNow.AddDays(25), // buffer > 10
                EndDate = DateTime.UtcNow.AddDays(30),   // duration > 3
                LocationId = 1,
                CampTypeId = 1,
                MinParticipants = 10,
                MaxParticipants = 20,
                MinAge = 10,
                MaxAge = 15,
                Description = "Desc",
                Place = "Place",
                Address = "Addr"
            };

            // mock empty db
            var emptyCamps = new List<Camp>();
            var mockSet = MockDbSetHelper.GetQueryableMockDbSet(emptyCamps);
            _mockCampRepo.Setup(r => r.GetQueryable()).Returns(mockSet.Object);

            // mock create behavior
            _mockCampRepo.Setup(r => r.CreateAsync(It.IsAny<Camp>()))
                .Callback<Camp>(c => emptyCamps.Add(c))
                .Returns(Task.CompletedTask);

            _mockUserContext.Setup(u => u.GetCurrentUserId()).Returns(1);

            // mock mapper
            _mockMapper.Setup(m => m.Map<Camp>(It.IsAny<CampRequestDto>())).Returns(new Camp { campId = 1, name = "Valid Camp" });
            _mockMapper.Setup(m => m.Map<CampResponseDto>(It.IsAny<Camp>())).Returns(new CampResponseDto { Name = "Valid Camp" });

            // act
            var result = await _campService.CreateCampAsync(request);

            // assert
            Assert.NotNull(result);
            Assert.Equal("Valid Camp", result.Name);

            _mockCampRepo.Verify(r => r.CreateAsync(It.IsAny<Camp>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        }

        // ==========================================
        // UT09 - SUBMIT CAMP FOR APPROVAL
        // ==========================================

        // UT09 - TEST CASE 1: Fail to submit (camp not found)
        [Fact]
        public async Task SubmitCamp_CampNotFound_ThrowsException()
        {
            // arrange
            var emptyList = new List<Camp>();
            var mockSet = MockDbSetHelper.GetQueryableMockDbSet(emptyList);
            _mockCampRepo.Setup(r => r.GetQueryable()).Returns(mockSet.Object);

            // act & assert
            var ex = await Assert.ThrowsAsync<Exception>(() => _campService.SubmitForApprovalAsync(99));
            Assert.Contains("Camp with ID 99 not found", ex.Message);
        }

        // UT09 - TEST CASE 2: Fail to submit (invalid status)
        [Fact]
        public async Task SubmitCamp_InvalidStatus_ThrowsBadRequest()
        {
            // arrange
            var camps = new List<Camp> { new Camp { campId = 1, status = "Published" } };
            var mockSet = MockDbSetHelper.GetQueryableMockDbSet(camps);
            _mockCampRepo.Setup(r => r.GetQueryable()).Returns(mockSet.Object);

            // act & assert
            var ex = await Assert.ThrowsAsync<BadRequestException>(() => _campService.SubmitForApprovalAsync(1));
            Assert.Contains("Chỉ có thể gửi phê duyệt từ trạng thái Draft", ex.Message);
        }

        // UT09 - TEST CASE 3: Fail to submit (no activities created)
        [Fact]
        public async Task SubmitCamp_NoActivities_ThrowsBadRequest()
        {
            // arrange
            var camps = new List<Camp> { new Camp { campId = 1, status = "Draft" } };
            var mockCampSet = MockDbSetHelper.GetQueryableMockDbSet(camps);
            _mockCampRepo.Setup(r => r.GetQueryable()).Returns(mockCampSet.Object);

            // mock no activities
            var emptyActivities = new List<Activity>();
            var mockActSet = MockDbSetHelper.GetQueryableMockDbSet(emptyActivities);
            _mockActivityRepo.Setup(r => r.GetQueryable()).Returns(mockActSet.Object);

            // act & assert
            var ex = await Assert.ThrowsAsync<BadRequestException>(() => _campService.SubmitForApprovalAsync(1));
            Assert.Contains("cần có ít nhất một hoạt động", ex.Message);

        }

        // UT09 - TEST CASE 4: Fail to submit (no staff or group assigned)
        [Fact]
        public async Task SubmitCamp_NoStaffOrGroup_ThrowsBadRequest()
        {
            // arrange
            var camps = new List<Camp>
            {
                new Camp {
                    campId = 1, status = "Draft",
                    CampStaffAssignments = new List<CampStaffAssignment>(), // empty
                    Groups = new List<Group>() // empty
                }
            };
            var mockCampSet = MockDbSetHelper.GetQueryableMockDbSet(camps);
            _mockCampRepo.Setup(r => r.GetQueryable()).Returns(mockCampSet.Object);

            // mock activities exist
            var activities = new List<Activity> { new Activity { campId = 1 } };
            var mockActSet = MockDbSetHelper.GetQueryableMockDbSet(activities);
            _mockActivityRepo.Setup(r => r.GetQueryable()).Returns(mockActSet.Object);

            // act & assert
            var ex = await Assert.ThrowsAsync<BadRequestException>(() => _campService.SubmitForApprovalAsync(1));
            Assert.Contains("cần có ít nhất một Group/Staff", ex.Message);
        }

        // UT09 - TEST CASE 5: Successful submission
        [Fact]
        public async Task SubmitCamp_ValidSubmission_ReturnsPendingApproval()
        {
            // arrange
            var camp = new Camp
            {
                campId = 1,
                status = "Draft",
                CampStaffAssignments = new List<CampStaffAssignment> { new CampStaffAssignment() },
                Groups = new List<Group>()
            };
            var camps = new List<Camp> { camp };

            var mockCampSet = MockDbSetHelper.GetQueryableMockDbSet(camps);
            _mockCampRepo.Setup(r => r.GetQueryable()).Returns(mockCampSet.Object);

            // mock activities
            var activities = new List<Activity> { new Activity { campId = 1 } };
            var mockActSet = MockDbSetHelper.GetQueryableMockDbSet(activities);
            _mockActivityRepo.Setup(r => r.GetQueryable()).Returns(mockActSet.Object);

            // mock update
            _mockCampRepo.Setup(r => r.UpdateAsync(It.IsAny<Camp>())).Returns(Task.CompletedTask);

            // mock mapper
            _mockMapper.Setup(m => m.Map<CampResponseDto>(camp))
                .Returns(new CampResponseDto { Status = "PendingApproval" });

            // act
            var result = await _campService.SubmitForApprovalAsync(1);

            // assert
            Assert.NotNull(result);
            Assert.Equal("PendingApproval", result.Status);

            _mockCampRepo.Verify(r => r.UpdateAsync(It.Is<Camp>(c => c.status == "PendingApproval")), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        }

        // ==========================================
        // UT10 - APPROVE CAMP (NEW)
        // ==========================================

        // UT10 - TEST CASE 1: Camp Not Found
        [Fact]
        public async Task ApproveCamp_CampNotFound_ThrowsNotFound()
        {
            // arrange
            var emptyList = new List<Camp>();
            var mockSet = MockDbSetHelper.GetQueryableMockDbSet(emptyList);
            _mockCampRepo.Setup(r => r.GetQueryable()).Returns(mockSet.Object);

            // act & assert
            var ex = await Assert.ThrowsAsync<NotFoundException>(() => _campService.TransitionCampStatusAsync(99, CampStatus.Published));
            Assert.Contains("Camp with ID 99 not found", ex.Message);
        }

        // UT10 - TEST CASE 2: Invalid Transition (Draft -> Published)
        [Fact]
        public async Task ApproveCamp_InvalidTransition_ThrowsBadRequest()
        {
            // arrange
            var camps = new List<Camp> { new Camp { campId = 1, status = "Draft" } };
            var mockSet = MockDbSetHelper.GetQueryableMockDbSet(camps);
            _mockCampRepo.Setup(r => r.GetQueryable()).Returns(mockSet.Object);

            // act & assert
            var ex = await Assert.ThrowsAsync<BadRequestException>(() => _campService.TransitionCampStatusAsync(1, CampStatus.Published));
            Assert.Contains("không hợp lệ theo flow", ex.Message);
        }

        // UT10 - TEST CASE 3: Valid Transition (Pending -> Published)
        [Fact]
        public async Task ApproveCamp_ValidTransition_ReturnsPublished()
        {
            // arrange
            var camp = new Camp { campId = 1, status = "PendingApproval" };
            var camps = new List<Camp> { camp };
            var mockCampSet = MockDbSetHelper.GetQueryableMockDbSet(camps);
            _mockCampRepo.Setup(r => r.GetQueryable()).Returns(mockCampSet.Object);

            // mock activity schedules (to be approved)
            var actSchedules = new List<ActivitySchedule>();
            var mockActSchSet = MockDbSetHelper.GetQueryableMockDbSet(actSchedules);
            _mockActScheduleRepo.Setup(r => r.GetQueryable()).Returns(mockActSchSet.Object);

            // mock routes & transport schedules
            var routes = new List<Route>();
            var mockRouteSet = MockDbSetHelper.GetQueryableMockDbSet(routes);
            _mockRouteRepo.Setup(r => r.GetQueryable()).Returns(mockRouteSet.Object);

            var transSchedules = new List<TransportSchedule>();
            var mockTransSet = MockDbSetHelper.GetQueryableMockDbSet(transSchedules);
            _mockTransScheduleRepo.Setup(r => r.GetQueryable()).Returns(mockTransSet.Object);

            // setup mapper
            _mockMapper.Setup(m => m.Map<CampResponseDto>(camp))
                .Returns(new CampResponseDto { Status = "Published" });

            // act
            var result = await _campService.TransitionCampStatusAsync(1, CampStatus.Published);

            // assert
            Assert.NotNull(result);
            Assert.Equal("Published", result.Status);

            _mockCampRepo.Verify(r => r.UpdateAsync(It.Is<Camp>(c => c.status == "Published")), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.AtLeastOnce);
        }

        // ==========================================
        // UT11 - REJECT CAMP 
        // ==========================================

        // UT11 - TEST CASE 1: Camp Not Found
        [Fact]
        public async Task RejectCamp_CampNotFound_ThrowsNotFound()
        {
            // arrange
            var emptyList = new List<Camp>();
            var mockSet = MockDbSetHelper.GetQueryableMockDbSet(emptyList);
            _mockCampRepo.Setup(r => r.GetQueryable()).Returns(mockSet.Object);

            // act & assert
            var ex = await Assert.ThrowsAsync<NotFoundException>(() => _campService.TransitionCampStatusAsync(99, CampStatus.Rejected));
            Assert.Contains("Camp with ID 99 not found", ex.Message);
        }

        // UT11 - TEST CASE 2: Invalid Transition (Draft -> Rejected)
        [Fact]
        public async Task RejectCamp_InvalidTransition_ThrowsBadRequest()
        {
            // arrange
            // Draft cannot go directly to Rejected (must go to PendingApproval first)
            var camps = new List<Camp> { new Camp { campId = 1, status = "Draft" } };
            var mockSet = MockDbSetHelper.GetQueryableMockDbSet(camps);
            _mockCampRepo.Setup(r => r.GetQueryable()).Returns(mockSet.Object);

            // act & assert
            var ex = await Assert.ThrowsAsync<BadRequestException>(() => _campService.TransitionCampStatusAsync(1, CampStatus.Rejected));
            Assert.Contains("không hợp lệ theo flow", ex.Message);
        }

        // UT11 - TEST CASE 3: Valid Transition (Pending -> Rejected)
        [Fact]
        public async Task RejectCamp_ValidTransition_ReturnsRejected()
        {
            // arrange
            var camp = new Camp { campId = 1, status = "PendingApproval" };
            var camps = new List<Camp> { camp };
            var mockCampSet = MockDbSetHelper.GetQueryableMockDbSet(camps);
            _mockCampRepo.Setup(r => r.GetQueryable()).Returns(mockCampSet.Object);

            // mock activity schedules (status: Draft -> Rejected)
            var actSchedules = new List<ActivitySchedule>
            {
                new ActivitySchedule { activityScheduleId = 1, activity = new Activity { campId = 1 }, status = "Draft" }
            };
            var mockActSchSet = MockDbSetHelper.GetQueryableMockDbSet(actSchedules);
            _mockActScheduleRepo.Setup(r => r.GetQueryable()).Returns(mockActSchSet.Object);

            // mock routes & transport schedules
            var routes = new List<Route> { new Route { routeId = 1, campId = 1 } };
            var mockRouteSet = MockDbSetHelper.GetQueryableMockDbSet(routes);
            _mockRouteRepo.Setup(r => r.GetQueryable()).Returns(mockRouteSet.Object);

            var transSchedules = new List<TransportSchedule>
            {
                new TransportSchedule { transportScheduleId = 1, routeId = 1, status = "Draft" }
            };
            var mockTransSet = MockDbSetHelper.GetQueryableMockDbSet(transSchedules);
            _mockTransScheduleRepo.Setup(r => r.GetQueryable()).Returns(mockTransSet.Object);

            // setup mapper
            _mockMapper.Setup(m => m.Map<CampResponseDto>(camp))
                .Returns(new CampResponseDto { Status = "Rejected" });

            // act
            var result = await _campService.TransitionCampStatusAsync(1, CampStatus.Rejected);

            // assert
            Assert.NotNull(result);
            Assert.Equal("Rejected", result.Status);

            // Verify Camp Updated
            _mockCampRepo.Verify(r => r.UpdateAsync(It.Is<Camp>(c => c.status == "Rejected")), Times.Once);

            // Verify Schedules Updated (Rejected)
            _mockActScheduleRepo.Verify(r => r.UpdateAsync(It.Is<ActivitySchedule>(s => s.status == "Rejected")), Times.Once);
            _mockTransScheduleRepo.Verify(r => r.UpdateAsync(It.Is<TransportSchedule>(s => s.status == "Rejected")), Times.Once);

            _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.AtLeastOnce);
        }

        // ==========================================
        // UT20 - DELETE CAMP
        // ==========================================

        // UT20 - TEST CASE 1: Camp Not Found
        [Fact]
        public async Task DeleteCamp_CampNotFound_ReturnsFalse()
        {
            // arrange
            _mockCampRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Camp)null);

            // act
            var result = await _campService.DeleteCampAsync(99);

            // assert
            Assert.False(result);
            _mockCampRepo.Verify(r => r.GetByIdAsync(99), Times.Once);
            _mockCampRepo.Verify(r => r.UpdateAsync(It.IsAny<Camp>()), Times.Never);
        }

        // UT20 - TEST CASE 2: Invalid Status (Not Draft)
        [Fact]
        public async Task DeleteCamp_InvalidStatus_ThrowsBusinessRuleException()
        {
            // arrange
            var camp = new Camp { campId = 1, status = "Published" };
            _mockCampRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(camp);

            // act & assert
            var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _campService.DeleteCampAsync(1));
            Assert.Contains("Chỉ có thể xóa trại ở trạng thái Draft", ex.Message);
            
            _mockCampRepo.Verify(r => r.UpdateAsync(It.IsAny<Camp>()), Times.Never);
        }

        // UT20 - TEST CASE 3: Successful Delete (Draft -> Canceled)
        [Fact]
        public async Task DeleteCamp_ValidStatus_ReturnsTrueAndUpdatesStatus()
        {
            // arrange
            var camp = new Camp { campId = 1, status = "Draft" };
            _mockCampRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(camp);
            _mockCampRepo.Setup(r => r.UpdateAsync(It.IsAny<Camp>())).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            // act
            var result = await _campService.DeleteCampAsync(1);

            // assert
            Assert.True(result);
            Assert.Equal("Canceled", camp.status);

            _mockCampRepo.Verify(r => r.UpdateAsync(It.Is<Camp>(c => c.status == "Canceled")), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        }
    }
}