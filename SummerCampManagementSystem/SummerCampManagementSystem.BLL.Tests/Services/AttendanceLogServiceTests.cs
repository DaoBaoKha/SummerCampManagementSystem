using AutoMapper;
using Moq;
using SummerCampManagementSystem.BLL.DTOs.AttendanceLog; 
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.BLL.Services;
using SummerCampManagementSystem.Core.Enums;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;
using SummerCampManagementSystem.DAL.UnitOfWork;

namespace SummerCampManagementSystem.BLL.Tests.Services
{
    public class AttendanceLogServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ICamperService> _mockCamperService;
        private readonly AttendanceLogService _service;

        // mock Repositories
        private readonly Mock<IAttendanceLogRepository> _mockAttendanceLogRepo;
        private readonly Mock<IActivityScheduleRepository> _mockScheduleRepo;
        private readonly Mock<IActivityRepository> _mockActivityRepo;
        private readonly Mock<ICamperRepository> _mockCamperRepo;

        public AttendanceLogServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _mockCamperService = new Mock<ICamperService>();

            // init Repos
            _mockAttendanceLogRepo = new Mock<IAttendanceLogRepository>();
            _mockScheduleRepo = new Mock<IActivityScheduleRepository>();
            _mockActivityRepo = new Mock<IActivityRepository>();
            _mockCamperRepo = new Mock<ICamperRepository>();

            // setup UnitOfWork
            _mockUnitOfWork.Setup(u => u.AttendanceLogs).Returns(_mockAttendanceLogRepo.Object);
            _mockUnitOfWork.Setup(u => u.ActivitySchedules).Returns(_mockScheduleRepo.Object);
            _mockUnitOfWork.Setup(u => u.Activities).Returns(_mockActivityRepo.Object);
            _mockUnitOfWork.Setup(u => u.Campers).Returns(_mockCamperRepo.Object);

            // init Service
            _service = new AttendanceLogService(
                _mockUnitOfWork.Object,
                _mockMapper.Object,
                _mockCamperService.Object
            );
        }

        // UT16 - TEST CASE 1: Schedule Not Found
        [Fact]
        public async Task CheckIn_ScheduleNotFound_ThrowsKeyNotFound()
        {
            // arrange
            var request = new AttendanceLogListRequestDto
            {
                ActivityScheduleId = 99,
                CamperIds = new List<int>()
            };

            _mockScheduleRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((ActivitySchedule?)null);

            // act & assert
            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.CoreActivityAttendanceAsync(request, staffId: 1));
            Assert.Contains("Activity Schedule not found", ex.Message);
        }

        // UT16 - TEST CASE 2: Activity Not Found
        [Fact]
        public async Task CheckIn_ActivityNotFound_ThrowsKeyNotFound()
        {
            // arrange
            var request = new AttendanceLogListRequestDto { ActivityScheduleId = 1 };

            var schedule = new ActivitySchedule { activityScheduleId = 1, activityId = 10 };
            _mockScheduleRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(schedule);

            _mockActivityRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync((Activity?)null);

            // act & assert
            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.CoreActivityAttendanceAsync(request, staffId: 1));
            Assert.Contains("does not have any activities", ex.Message);
        }

        // UT16 - TEST CASE 3: Successful Check-In (Create Attendance Logs)
        [Fact]
        public async Task CheckIn_ValidData_CreatesLogsAndUpdatesStatus()
        {
            // arrange
            var request = new AttendanceLogListRequestDto
            {
                ActivityScheduleId = 1,
                CamperIds = new List<int> { 101, 102 },
                participantStatus = ParticipationStatus.Present
            };

            var schedule = new ActivitySchedule { activityScheduleId = 1, activityId = 10, status = "Scheduled" };
            var activity = new Activity { activityId = 10 };

            _mockScheduleRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(schedule);
            _mockActivityRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(activity);

            // mock existing campers
            _mockCamperRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new Camper());

            // mock create: use callback to capture created logs
            _mockAttendanceLogRepo.Setup(r => r.CreateAsync(It.IsAny<AttendanceLog>())).Returns(Task.CompletedTask);

            // mock Update Activity Schedule
            _mockScheduleRepo.Setup(r => r.UpdateAsync(It.IsAny<ActivitySchedule>())).Returns(Task.CompletedTask);

            // act
            await _service.CoreActivityAttendanceAsync(request, staffId: 5);

            // assert
            // verify create logs
            _mockAttendanceLogRepo.Verify(r => r.CreateAsync(It.Is<AttendanceLog>(
                l => (l.camperId == 101 || l.camperId == 102) &&
                     l.activityScheduleId == 1 &&
                     l.staffId == 5 &&
                     l.participantStatus == "Present"
            )), Times.Exactly(2));

            // verify update schedule status
            Assert.Equal("AttendanceChecked", schedule.status);
            _mockScheduleRepo.Verify(r => r.UpdateAsync(It.IsAny<ActivitySchedule>()), Times.Once);

            _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        }

        // ==========================================
        // UT17 - CHECK OUT (Attendance for Check-out Activity)
        // ==========================================

        // UT17 - TEST CASE 1: Schedule Not Found
        [Fact]
        public async Task CheckOut_ScheduleNotFound_ThrowsKeyNotFound()
        {
            // arrange
            var request = new AttendanceLogListRequestDto
            {
                ActivityScheduleId = 999, // id not exist
                CamperIds = new List<int> { 101 }
            };

            _mockScheduleRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((ActivitySchedule?)null);

            // act & assert
            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.CoreActivityAttendanceAsync(request, staffId: 1));
            Assert.Contains("Activity Schedule not found", ex.Message);
        }

        // UT17 - TEST CASE 2: Activity Not Found
        [Fact]
        public async Task CheckOut_ActivityNotFound_ThrowsKeyNotFound()
        {
            // arrange
            var request = new AttendanceLogListRequestDto { ActivityScheduleId = 2 };
            var schedule = new ActivitySchedule { activityScheduleId = 2, activityId = 20 };

            _mockScheduleRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(schedule);
            _mockActivityRepo.Setup(r => r.GetByIdAsync(20)).ReturnsAsync((Activity?)null);

            // act & assert
            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.CoreActivityAttendanceAsync(request, staffId: 1));
            Assert.Contains("does not have any activities", ex.Message);
        }

        // UT17 - TEST CASE 3: Successful Check-Out
        [Fact]
        public async Task CheckOut_ValidData_CreatesLogs()
        {
            // arrange
            var request = new AttendanceLogListRequestDto
            {
                ActivityScheduleId = 2,
                CamperIds = new List<int> { 101 },
                participantStatus = ParticipationStatus.Present // present to check out
            };

            var schedule = new ActivitySchedule { activityScheduleId = 2, activityId = 20, status = "Scheduled" };
            var activity = new Activity { activityId = 20, activityType = "Checkout" }; // type Checkout

            _mockScheduleRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(schedule);
            _mockActivityRepo.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(activity);

            // mock camper
            _mockCamperRepo.Setup(r => r.GetByIdAsync(101)).ReturnsAsync(new Camper());

            // mock create & update
            _mockAttendanceLogRepo.Setup(r => r.CreateAsync(It.IsAny<AttendanceLog>())).Returns(Task.CompletedTask);
            _mockScheduleRepo.Setup(r => r.UpdateAsync(It.IsAny<ActivitySchedule>())).Returns(Task.CompletedTask);

            // act
            await _service.CoreActivityAttendanceAsync(request, staffId: 5);

            // assert
            // verify create log
            _mockAttendanceLogRepo.Verify(r => r.CreateAsync(It.Is<AttendanceLog>(
                l => l.camperId == 101 &&
                     l.activityScheduleId == 2 &&
                     l.staffId == 5
            )), Times.Once);

            // verify update schedule status
            Assert.Equal("AttendanceChecked", schedule.status);
            _mockScheduleRepo.Verify(r => r.UpdateAsync(It.IsAny<ActivitySchedule>()), Times.Once);

            _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        }

        // ==========================================
        // UT18 - CHECK ACTIVITY (Specific Activity Attendance)
        // ==========================================

        // UT18 - TEST CASE 1: Invalid Activity Type (Logic check: coreActivityId != null)
        [Fact]
        public async Task CheckActivity_InvalidType_ThrowsInvalidOperation()
        {
            // arrange
            var request = new AttendanceLogListRequestDto { ActivityScheduleId = 3 };

            // assume schedule has coreActivityId set
            var schedule = new ActivitySchedule
            {
                activityScheduleId = 3,
                activityId = 30,
                coreActivityId = 123 // has value -> error
            };
            var activity = new Activity { activityId = 30 };

            _mockScheduleRepo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(schedule);
            _mockActivityRepo.Setup(r => r.GetByIdAsync(30)).ReturnsAsync(activity);

            // act & assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.CoreActivityAttendanceAsync(request, staffId: 1));
            Assert.Contains("This is not a core Activity Schedule", ex.Message);
        }

        // UT18 - TEST CASE 2: Activity Not Found (Data Integrity Check)
        [Fact]
        public async Task CheckActivity_ActivityNotFound_ThrowsKeyNotFound()
        {
            // arrange
            var request = new AttendanceLogListRequestDto { ActivityScheduleId = 3 };

            // schedule with no coreActivityId
            var schedule = new ActivitySchedule { activityScheduleId = 3, activityId = 999 };

            _mockScheduleRepo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(schedule);
            _mockActivityRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Activity?)null);

            // act & assert
            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.CoreActivityAttendanceAsync(request, staffId: 1));
            Assert.Contains("does not have any activities", ex.Message);
        }

        // UT18 - TEST CASE 3: Successful Activity Attendance
        [Fact]
        public async Task CheckActivity_ValidData_CreatesLogs()
        {
            // arrange
            var request = new AttendanceLogListRequestDto
            {
                ActivityScheduleId = 3,
                CamperIds = new List<int> { 101, 105 },
                participantStatus = ParticipationStatus.Present
            };

            // valid schedule with no coreActivityId
            var schedule = new ActivitySchedule
            {
                activityScheduleId = 3,
                activityId = 30,
                coreActivityId = null,
                status = "Scheduled"
            };
            var activity = new Activity { activityId = 30 };

            _mockScheduleRepo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(schedule);
            _mockActivityRepo.Setup(r => r.GetByIdAsync(30)).ReturnsAsync(activity);

            // mock campers
            _mockCamperRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new Camper());

            // mock create
            _mockAttendanceLogRepo.Setup(r => r.CreateAsync(It.IsAny<AttendanceLog>())).Returns(Task.CompletedTask);
            _mockScheduleRepo.Setup(r => r.UpdateAsync(It.IsAny<ActivitySchedule>())).Returns(Task.CompletedTask);

            // act
            await _service.CoreActivityAttendanceAsync(request, staffId: 5);

            // assert
            // verify create logs
            _mockAttendanceLogRepo.Verify(r => r.CreateAsync(It.Is<AttendanceLog>(
                l => l.activityScheduleId == 3 && l.participantStatus == "Present"
            )), Times.Exactly(2));

            // verify update schedule 
            _mockScheduleRepo.Verify(r => r.UpdateAsync(It.IsAny<ActivitySchedule>()), Times.Once);

            _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        }
    }
}