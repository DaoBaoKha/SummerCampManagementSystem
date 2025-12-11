using AutoMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using SummerCampManagementSystem.BLL.DTOs.User;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.BLL.Services;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace SummerCampManagementSystem.BLL.Tests.Services
{
    public class UserServiceTests
    {
        // mock objects
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly Mock<IMemoryCache> _mockCache;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<UserService>> _mockLogger;

        private readonly UserService _userService;

        public UserServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockConfig = new Mock<IConfiguration>();
            _mockEmailService = new Mock<IEmailService>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<UserService>>();

            // cache mock
            _mockCache = new Mock<IMemoryCache>();
            var mockCacheEntry = new Mock<ICacheEntry>();
            _mockCache.Setup(m => m.CreateEntry(It.IsAny<object>())).Returns(mockCacheEntry.Object);

            _userService = new UserService(
                _mockUnitOfWork.Object,
                _mockConfig.Object,
                _mockCache.Object,
                _mockEmailService.Object,
                _mockMapper.Object,
                _mockLogger.Object
            );
        }

        // UT01 - TEST CASE 1: Fail to register (email already exists)
        [Fact]
        public async Task RegisterAsync_EmailExists_ReturnsNull()
        {
            // ARRANGE
            var request = new RegisterUserRequestDto
            {
                Email = "duplicate@test.com",
                Password = "Password123",
                FirstName = "Dao Bao",
                LastName = "Khaa",
                PhoneNumber = "0901234567"
            };

            _mockUnitOfWork.Setup(u => u.Users.GetUserByEmail(request.Email))
                .ReturnsAsync(new UserAccount { email = request.Email });

            var result = await _userService.RegisterAsync(request);

            // ASSERT (check)
            Assert.Null(result); // result expected: null
        }

        // UT01 - TEST CASE 2: Successful registration (new email)
        [Fact]
        public async Task RegisterAsync_NewEmail_ReturnsSuccessAndSendsOtp()
        {
            // ARRANGE
            var request = new RegisterUserRequestDto
            {
                Email = "newuser@test.com",
                Password = "Password123",
                FirstName = "Tran",
                LastName = "Thi B",
                PhoneNumber = "0987654321"
            };

            _mockUnitOfWork.Setup(u => u.Users.GetUserByEmail(request.Email))
                .ReturnsAsync((UserAccount?)null);

            _mockConfig.Setup(c => c["AppSettings:DefaultAvatarUrl"]).Returns("default_avatar.png");

            // ACT
            var result = await _userService.RegisterAsync(request);

            // ASSERT
            Assert.NotNull(result); 
            Assert.Equal("Đăng ký thành công! OTP đã được gửi tới email của bạn.", result.Message);

            // check if create user method was called once
            _mockUnitOfWork.Verify(u => u.Users.CreateAsync(It.IsAny<UserAccount>()), Times.Once);

            // check if commit method was called once
            _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Once);

            // check if otp is sent
            _mockEmailService.Verify(e => e.SendOtpEmailAsync(
                request.Email,
                It.IsAny<string>(), 
                "Activation"
            ), Times.Once);
        }
    }
}