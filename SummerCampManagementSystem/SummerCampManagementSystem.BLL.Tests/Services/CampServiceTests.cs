using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using SummerCampManagementSystem.BLL.DTOs.Camp;
using SummerCampManagementSystem.BLL.Exceptions;
using SummerCampManagementSystem.BLL.Helpers;
using SummerCampManagementSystem.BLL.Services;
using SummerCampManagementSystem.BLL.Tests.Helpers;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;

namespace SummerCampManagementSystem.BLL.Tests.Services
{
    public class CampServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IUserContextService> _mockUserContext;
        private readonly Mock<ILogger<CampService>> _mockLogger;

        private readonly CampService _campService;

        public CampServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _mockUserContext = new Mock<IUserContextService>();
            _mockLogger = new Mock<ILogger<CampService>>();

            _campService = new CampService(
                _mockUnitOfWork.Object,
                _mockMapper.Object,
                _mockUserContext.Object,
                _mockLogger.Object
            );
        }

        // UT02 - TEST CASE 1: Fail (Date invalid)
        [Fact]
        public async Task CreateCamp_RegEndAfterStart_ThrowsBadRequest()
        {
            var request = new CampRequestDto
            {
                Name = "Camp Date Invalid",
                StartDate = DateTime.UtcNow.AddDays(20),
                RegistrationEndDate = DateTime.UtcNow.AddDays(21), // Error
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

            var ex = await Assert.ThrowsAsync<BadRequestException>(() => _campService.CreateCampAsync(request));
            Assert.Contains("Ngày đóng đăng ký phải trước ngày bắt đầu", ex.Message);
        }

        // UT02 - TEST CASE 2: Fail (Buffer time < 10 days)
        [Fact]
        public async Task CreateCamp_BufferTimeTooShort_ThrowsBadRequest()
        {
            var request = new CampRequestDto
            {
                Name = "Camp Buffer Short",
                RegistrationStartDate = DateTime.UtcNow,
                RegistrationEndDate = DateTime.UtcNow.AddDays(5),
                StartDate = DateTime.UtcNow.AddDays(10), // Only 5 days buffer
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

            var ex = await Assert.ThrowsAsync<BadRequestException>(() => _campService.CreateCampAsync(request));
            Assert.Contains("Ngày đóng đăng ký phải trước ngày bắt đầu ít nhất 10 ngày", ex.Message);
        }

        // UT02 - TEST CASE 3: Fail (Duration < 3 days)
        [Fact]
        public async Task CreateCamp_DurationTooShort_ThrowsBadRequest()
        {
            var request = new CampRequestDto
            {
                Name = "Camp Duration Short",
                RegistrationStartDate = DateTime.UtcNow,
                RegistrationEndDate = DateTime.UtcNow.AddDays(10),
                StartDate = DateTime.UtcNow.AddDays(25),
                EndDate = DateTime.UtcNow.AddDays(26), // Only 1 day
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

            var ex = await Assert.ThrowsAsync<BadRequestException>(() => _campService.CreateCampAsync(request));
            Assert.Contains("Thời lượng trại phải kéo dài ít nhất 3 ngày", ex.Message);
        }

        // UT02 - TEST CASE 4: Fail (Overlap Location)
        [Fact]
        public async Task CreateCamp_LocationOverlap_ThrowsBadRequest()
        {
            var request = new CampRequestDto
            {
                Name = "Camp Overlap",
                RegistrationStartDate = DateTime.UtcNow,
                RegistrationEndDate = DateTime.UtcNow.AddDays(10),
                StartDate = DateTime.UtcNow.AddDays(25),
                EndDate = DateTime.UtcNow.AddDays(30),
                LocationId = 99, // Overlap
                CampTypeId = 1,
                MinParticipants = 10,
                MaxParticipants = 20,
                MinAge = 10,
                MaxAge = 15,
                Description = "Desc",
                Place = "Place",
                Address = "Addr"
            };

            var existingCamps = new List<Camp>
            {
                new Camp {
                    campId = 1, locationId = 99, status = "Published",
                    startDate = DateTime.UtcNow.AddDays(24), endDate = DateTime.UtcNow.AddDays(31)
                }
            };

            var mockSet = MockDbSetHelper.GetQueryableMockDbSet(existingCamps);
            _mockUnitOfWork.Setup(u => u.Camps.GetQueryable()).Returns(mockSet.Object);

            var ex = await Assert.ThrowsAsync<BadRequestException>(() => _campService.CreateCampAsync(request));
            Assert.Contains("Địa điểm này đã có Camp", ex.Message);
        }

        // UT02 - TEST CASE 5: Successful creation (Happy path)
        [Fact]
        public async Task CreateCamp_ValidData_ReturnsResponse()
        {
            // ARRANGE
            var request = new CampRequestDto
            {
                Name = "Camp Valid",
                RegistrationStartDate = DateTime.UtcNow,
                RegistrationEndDate = DateTime.UtcNow.AddDays(10),
                StartDate = DateTime.UtcNow.AddDays(25), // Buffer > 10
                EndDate = DateTime.UtcNow.AddDays(30),   // Duration > 3
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

            // create a mock DbSet to hold Camps
            var mockDbData = new List<Camp>();

            // setup dbset to return queryable data
            var mockSet = MockDbSetHelper.GetQueryableMockDbSet(mockDbData);
            _mockUnitOfWork.Setup(u => u.Camps.GetQueryable()).Returns(mockSet.Object);

            // when CreateAsync is called, add to mockDbData
            _mockUnitOfWork.Setup(u => u.Camps.CreateAsync(It.IsAny<Camp>()))
                .Callback<Camp>((camp) => mockDbData.Add(camp)) 
                .Returns(Task.CompletedTask);

            _mockUserContext.Setup(u => u.GetCurrentUserId()).Returns(1);

            _mockMapper.Setup(m => m.Map<Camp>(It.IsAny<CampRequestDto>()))
                .Returns((CampRequestDto src) => new Camp
                {
                    name = src.Name,
                    startDate = src.StartDate,
                    endDate = src.EndDate,
                    campId = 1 
                });

            _mockMapper.Setup(m => m.Map<CampResponseDto>(It.IsAny<Camp>()))
                .Returns((Camp src) => new CampResponseDto { Name = src.name });

            // act
            var result = await _campService.CreateCampAsync(request);

            // assert
            Assert.NotNull(result);
            Assert.Equal("Camp Valid", result.Name);

            _mockUnitOfWork.Verify(u => u.Camps.CreateAsync(It.IsAny<Camp>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        }
    }
}