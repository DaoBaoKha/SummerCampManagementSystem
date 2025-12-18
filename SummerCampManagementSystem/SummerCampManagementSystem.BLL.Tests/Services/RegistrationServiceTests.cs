using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using SummerCampManagementSystem.BLL.DTOs.Registration;
using SummerCampManagementSystem.BLL.Helpers;
using SummerCampManagementSystem.BLL.Services;
using SummerCampManagementSystem.BLL.Tests.Helpers;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;
using SummerCampManagementSystem.DAL.UnitOfWork;
using SummerCampManagementSystem.BLL.Exceptions;

namespace SummerCampManagementSystem.BLL.Tests.Services
{
    public class RegistrationServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IValidationService> _mockValidationService;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IUserContextService> _mockUserContext;
        private readonly RegistrationService _service;

        // mock repos
        private readonly Mock<ICampRepository> _mockCampRepo;
        private readonly Mock<IRegistrationRepository> _mockRegistrationRepo;
        private readonly Mock<ICamperRepository> _mockCamperRepo;
        private readonly Mock<IRegistrationCamperRepository> _mockRegCamperRepo;

        // db context mock
        private readonly Mock<CampEaseDatabaseContext> _mockDbContext;

        public RegistrationServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockValidationService = new Mock<IValidationService>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockMapper = new Mock<IMapper>();
            _mockUserContext = new Mock<IUserContextService>();

            // init repos
            _mockCampRepo = new Mock<ICampRepository>();
            _mockRegistrationRepo = new Mock<IRegistrationRepository>();
            _mockCamperRepo = new Mock<ICamperRepository>();
            _mockRegCamperRepo = new Mock<IRegistrationCamperRepository>(); 

            // setup UnitOfWork
            _mockUnitOfWork.Setup(u => u.Camps).Returns(_mockCampRepo.Object);
            _mockUnitOfWork.Setup(u => u.Registrations).Returns(_mockRegistrationRepo.Object);
            _mockUnitOfWork.Setup(u => u.Campers).Returns(_mockCamperRepo.Object);
            _mockUnitOfWork.Setup(u => u.RegistrationCampers).Returns(_mockRegCamperRepo.Object);

            // setup DbContext mock
            var options = new DbContextOptions<CampEaseDatabaseContext>();
            _mockDbContext = new Mock<CampEaseDatabaseContext>(options);
            _mockUnitOfWork.Setup(u => u.GetDbContext()).Returns(_mockDbContext.Object);

            _service = new RegistrationService(
                _mockUnitOfWork.Object,
                _mockValidationService.Object,
                null, // payOS passed as null
                _mockConfiguration.Object,
                _mockMapper.Object,
                _mockUserContext.Object
            );
        }

        // ==========================================
        // UT12 - REGISTER CAMPER
        // ==========================================

        // UT12 - TEST CASE 1: Camp Not Found
        [Fact]
        public async Task CreateReg_CampNotFound_ThrowsKeyNotFound()
        {
            // arrange
            var request = new CreateRegistrationRequestDto { CampId = 99 };
            _mockCampRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Camp?)null);

            // act & assert
            var ex = await Assert.ThrowsAsync<NotFoundException>(() => _service.CreateRegistrationAsync(request));
            Assert.Contains("Camp with ID 99 not found", ex.Message);
        }

        // UT12 - TEST CASE 2: Unauthorized (No User ID)
        [Fact]
        public async Task CreateReg_Unauthorized_ThrowsUnauthorizedAccess()
        {
            // arrange
            var request = new CreateRegistrationRequestDto { CampId = 1 };
            _mockCampRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Camp());
            _mockUserContext.Setup(u => u.GetCurrentUserId()).Returns((int?)null);

            // act & assert
            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.CreateRegistrationAsync(request));
            Assert.Contains("Cannot get user ID from token", ex.Message);
        }

        // UT12 - TEST CASE 3: Camper Already Registered
        [Fact]
        public async Task CreateReg_CamperAlreadyRegistered_ThrowsInvalidOperation()
        {
            // arrange
            var request = new CreateRegistrationRequestDto
            {
                CampId = 1,
                CamperIds = new List<int> { 100 }
            };

            _mockCampRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Camp());
            _mockUserContext.Setup(u => u.GetCurrentUserId()).Returns(1);

            // mock existing registration in DB
            _mockRegistrationRepo.Setup(r => r.IsCamperRegisteredAsync(1, 100)).ReturnsAsync(true);

            // mock camper info for error message
            _mockCamperRepo.Setup(r => r.GetByIdAsync(100)).ReturnsAsync(new Camper { camperName = "Be Bi" });

            // act & assert
            var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.CreateRegistrationAsync(request));
            Assert.Contains("đã được đăng ký tham gia", ex.Message);
        }

        // UT12 - TEST CASE 4: Camper Not Found
        [Fact]
        public async Task CreateReg_CamperNotFound_ThrowsKeyNotFound()
        {
            // arrange
            var request = new CreateRegistrationRequestDto
            {
                CampId = 1,
                CamperIds = new List<int> { 99 }
            };

            _mockCampRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Camp());
            _mockUserContext.Setup(u => u.GetCurrentUserId()).Returns(1);

            // mock no existing registrations
            var emptyList = new List<Registration>();
            var mockSet = MockDbSetHelper.GetQueryableMockDbSet(emptyList);
            _mockRegistrationRepo.Setup(r => r.GetQueryable()).Returns(mockSet.Object);

            // mock camper not found
            _mockCamperRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Camper?)null);

            // act & assert
            var ex = await Assert.ThrowsAsync<NotFoundException>(() => _service.CreateRegistrationAsync(request));
            Assert.Contains("Camper with ID 99 not found", ex.Message);
        }

        // UT12 - TEST CASE 5: Success
        [Fact]
        public async Task CreateReg_ValidData_ReturnsResponse()
        {
            // arrange
            var request = new CreateRegistrationRequestDto
            {
                CampId = 1,
                CamperIds = new List<int> { 101 },
                Note = "Test"
            };

            _mockCampRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Camp { price = 1000 });
            _mockUserContext.Setup(u => u.GetCurrentUserId()).Returns(1);

            // mock no duplicates
            var emptyList = new List<Registration>();
            var mockSet = MockDbSetHelper.GetQueryableMockDbSet(emptyList);
            _mockRegistrationRepo.Setup(r => r.GetQueryable()).Returns(mockSet.Object);

            // mock camper exists
            _mockCamperRepo.Setup(r => r.GetByIdAsync(101)).ReturnsAsync(new Camper());

            // mock create
            _mockRegistrationRepo.Setup(r => r.CreateAsync(It.IsAny<Registration>()))
                .Callback<Registration>(r => {
                    r.registrationId = 10;
                    r.status = "PendingApproval"; // Assign status
                    r.RegistrationCampers = new List<RegistrationCamper>(); // ensure not null
                    emptyList.Add(r); 
                })
                .Returns(Task.CompletedTask);

            // mock mapper
            _mockMapper.Setup(m => m.Map<RegistrationResponseDto>(It.IsAny<Registration>()))
                .Returns(new RegistrationResponseDto { registrationId = 10, Status = "PendingApproval" });

            // mock GetWithCampersAsync for the return
            // mock GetWithCampersAsync for the return
            var createdReg = new Registration { registrationId = 10, status = "PendingApproval", RegistrationCampers = new List<RegistrationCamper>() };
             _mockRegistrationRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(createdReg);
             _mockRegistrationRepo.Setup(r => r.GetWithCampersAsync(10)).ReturnsAsync(createdReg);
             // Ensure CreateAsync callback sets the ID on the object passed to it
             _mockRegistrationRepo.Setup(r => r.CreateAsync(It.IsAny<Registration>()))
                 .Callback<Registration>(r => {
                     r.registrationId = 10;
                     // r.status and campers are set in service
                 })
                 .Returns(Task.CompletedTask);

            // act
            var result = await _service.CreateRegistrationAsync(request);

            // assert
            Assert.NotNull(result);
            Assert.Equal(10, result.registrationId);

            _mockRegistrationRepo.Verify(r => r.CreateAsync(It.IsAny<Registration>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        }

        // ==========================================
        // UT13 - APPROVE REGISTRATION
        // ==========================================

        // UT13 - TEST CASE 1: Registration Not Found
        [Fact]
        public async Task ApproveReg_NotFound_ThrowsKeyNotFound()
        {
            // arrange
            var emptyList = new List<Registration>();
            var mockSet = MockDbSetHelper.GetQueryableMockDbSet(emptyList);
            _mockRegistrationRepo.Setup(r => r.GetQueryable()).Returns(mockSet.Object);

            // act & assert
            var ex = await Assert.ThrowsAsync<NotFoundException>(() => _service.ApproveRegistrationAsync(99));
            Assert.Contains("Registration with ID 99 not found", ex.Message);
        }

        // UT13 - TEST CASE 2: Invalid Status (Not PendingApproval)
        [Fact]
        public async Task ApproveReg_InvalidStatus_ThrowsInvalidOperation()
        {
            // arrange
            var regs = new List<Registration>
            {
                new Registration
                {
                    registrationId = 1,
                    status = "Draft",
                    RegistrationCampers = new List<RegistrationCamper>()
                }
            };
            var mockSet = MockDbSetHelper.GetQueryableMockDbSet(regs);
            _mockRegistrationRepo.Setup(r => r.GetQueryable()).Returns(mockSet.Object);
            _mockRegistrationRepo.Setup(r => r.GetWithCampersAsync(1)).ReturnsAsync(regs[0]);

            // act & assert
            // act & assert
            var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.ApproveRegistrationAsync(1));
            Assert.Contains("Only 'PendingApproval' registrations can be approved", ex.Message);
        }

        // UT13 - TEST CASE 3: Successful Approval
        [Fact]
        public async Task ApproveReg_ValidData_ReturnsApproved()
        {
            // arrange
            // Ensure ID 1 exists
            var reg = new Registration
            {
                registrationId = 1,
                status = "PendingApproval",
                RegistrationCampers = new List<RegistrationCamper>
                {
                    new RegistrationCamper { camperId = 101, status = "PendingApproval" },
                    new RegistrationCamper { camperId = 102, status = "PendingApproval" }
                }
            };
            
            // Fix: Mock GetByIdAsync specifically
            _mockRegistrationRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(reg);
            _mockRegistrationRepo.Setup(r => r.GetWithCampersAsync(1)).ReturnsAsync(reg);

            // Mock updates
            _mockRegistrationRepo.Setup(r => r.UpdateAsync(It.IsAny<Registration>())).Returns(Task.CompletedTask);
            _mockRegCamperRepo.Setup(r => r.UpdateAsync(It.IsAny<RegistrationCamper>())).Returns(Task.CompletedTask);

            // Mock Mapper
            _mockMapper.Setup(m => m.Map<RegistrationResponseDto>(reg))
                .Returns(new RegistrationResponseDto { registrationId = 1, Status = "Approved" });

            // act
            var result = await _service.ApproveRegistrationAsync(1);

            // assert
            Assert.NotNull(result);
            Assert.Equal("Approved", result.Status);

            // Verify Registration Updated
            _mockRegistrationRepo.Verify(r => r.UpdateAsync(It.Is<Registration>(x => x.status == "Approved")), Times.Once);

            // Verify Camper Links Updated (should be called twice for 2 campers)
            _mockRegCamperRepo.Verify(r => r.UpdateAsync(It.Is<RegistrationCamper>(c => c.status == "Approved")), Times.Exactly(2));

            // Verify Commit
            _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        }

        // ==========================================
        // UT14 - REJECT/UPDATE REGISTRATION (NEW)
        // ==========================================

        // UT14 - TEST CASE 1: Registration Not Found
        [Fact]
        public async Task UpdateReg_NotFound_ThrowsKeyNotFound()
        {
            // arrange
            var request = new UpdateRegistrationRequestDto { CampId = 1, CamperIds = new List<int>() };
            var emptyList = new List<Registration>();
            var mockSet = MockDbSetHelper.GetQueryableMockDbSet(emptyList);
            _mockRegistrationRepo.Setup(r => r.GetQueryable()).Returns(mockSet.Object);

            // act & assert
            var ex = await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateRegistrationAsync(99, request));
            Assert.Contains("Không tìm thấy đơn ID 99", ex.Message);
        }

        // UT14 - TEST CASE 2: Invalid Status (Cannot update Rejected/Cancelled)
        [Fact]
        public async Task UpdateReg_InvalidStatus_ThrowsInvalidOperation()
        {
            // arrange
            var request = new UpdateRegistrationRequestDto { CampId = 1, CamperIds = new List<int>() };
            var regs = new List<Registration>
            {
                new Registration { registrationId = 1, status = "Rejected", RegistrationCampers = new List<RegistrationCamper>() }
            };
            var mockSet = MockDbSetHelper.GetQueryableMockDbSet(regs);
            _mockRegistrationRepo.Setup(r => r.GetQueryable()).Returns(mockSet.Object);
            // Fix: Mock GetByIdAsync explicitly
            _mockRegistrationRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(regs[0]);

            // act & assert
            var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.UpdateRegistrationAsync(1, request));
            Assert.Contains("Cannot update registration with status", ex.Message);
        }

        // UT14 - TEST CASE 3: Status Reset (Approved -> PendingApproval)
        [Fact]
        public async Task UpdateReg_ApprovedToPending_ResetStatus()
        {
            // arrange
            int regId = 1;
            var request = new UpdateRegistrationRequestDto { CampId = 2, CamperIds = new List<int>(), Note = "Updated" };

            var reg = new Registration { registrationId = regId, status = "Approved", campId = 1 };
            var regs = new List<Registration> { reg };

            // Mock Repo Queryable
            var mockRegSet = MockDbSetHelper.GetQueryableMockDbSet(regs);
            _mockRegistrationRepo.Setup(r => r.GetQueryable()).Returns(mockRegSet.Object);
            // Fix: Mock GetByIdAsync explicitly
            _mockRegistrationRepo.Setup(r => r.GetByIdAsync(regId)).ReturnsAsync(reg);

            // Setup DbContext Mocks for direct access
            var dbRegSet = MockDbSetHelper.GetQueryableMockDbSet(regs);
            _mockDbContext.Setup(c => c.Registrations).Returns(dbRegSet.Object);

            var dbLinkSet = MockDbSetHelper.GetQueryableMockDbSet(new List<RegistrationCamper>());
            _mockDbContext.Setup(c => c.RegistrationCampers).Returns(dbLinkSet.Object);

            // Mock Camp Check
            _mockCampRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new Camp { campId = 2 });

            // Mock Mapper
            _mockMapper.Setup(m => m.Map<RegistrationResponseDto>(It.IsAny<Registration>()))
                .Returns(new RegistrationResponseDto { registrationId = 1, Status = "PendingApproval" });

            // act
            var result = await _service.UpdateRegistrationAsync(regId, request);

            // assert
            Assert.NotNull(result);
            Assert.Equal("PendingApproval", result.Status);
            // Verify DB update
            Assert.Equal("PendingApproval", reg.status); // Check entity state
            _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        }

        // UT14 - TEST CASE 4: Normal Update (Pending -> Pending)
        [Fact]
        public async Task UpdateReg_PendingUpdates_StatusRemainsPending()
        {
            // arrange
            int regId = 1;
            var request = new UpdateRegistrationRequestDto { CampId = 1, CamperIds = new List<int>(), Note = "New Note" };

            var reg = new Registration { registrationId = regId, status = "PendingApproval", campId = 1, note = "Old" };
            var regs = new List<Registration> { reg };

            // Mock Repo
            var mockRegSet = MockDbSetHelper.GetQueryableMockDbSet(regs);
            _mockRegistrationRepo.Setup(r => r.GetQueryable()).Returns(mockRegSet.Object);
            // Fix: Mock GetByIdAsync explicitly
            _mockRegistrationRepo.Setup(r => r.GetByIdAsync(regId)).ReturnsAsync(reg);

            // Mock DbContext
            var dbRegSet = MockDbSetHelper.GetQueryableMockDbSet(regs);
            _mockDbContext.Setup(c => c.Registrations).Returns(dbRegSet.Object);

            var dbLinkSet = MockDbSetHelper.GetQueryableMockDbSet(new List<RegistrationCamper>());
            _mockDbContext.Setup(c => c.RegistrationCampers).Returns(dbLinkSet.Object);

            // Mock Camp Check
            _mockCampRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Camp { campId = 1 });

            // Mock Mapper
            _mockMapper.Setup(m => m.Map<RegistrationResponseDto>(It.IsAny<Registration>()))
                .Returns(new RegistrationResponseDto { registrationId = 1, Status = "PendingApproval" });

            // act
            var result = await _service.UpdateRegistrationAsync(regId, request);

            // assert
            Assert.NotNull(result);
            Assert.Equal("PendingApproval", result.Status);
            Assert.Equal("New Note", reg.note); // Check entity update
            _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        }

        // ==========================================
        // UT19 - CANCEL REGISTRATION (DeleteRegistrationAsync)
        // ==========================================

        // UT19 - TEST CASE 1: Registration Not Found
        [Fact]
        public async Task DeleteReg_NotFound_ThrowsKeyNotFound()
        {
            // Arrange
            _mockUserContext.Setup(u => u.GetCurrentUserId()).Returns(1);
            _mockRegistrationRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Registration?)null);

            // Act & Assert 
            var ex = await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteRegistrationAsync(99));
            Assert.Contains("not found", ex.Message);
        }

        // UT19 - TEST CASE 2: Unauthorized 
        [Fact]
        public async Task DeleteReg_NotOwner_ThrowsUnauthorized()
        {
            // arrange
            int regId = 1;
            int currentUserId = 100;
            int ownerId = 200;

            var registration = new Registration { registrationId = regId, userId = ownerId };

            _mockUserContext.Setup(u => u.GetCurrentUserId()).Returns(currentUserId);
            _mockRegistrationRepo.Setup(r => r.GetByIdAsync(regId)).ReturnsAsync(registration);

            // act & assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.DeleteRegistrationAsync(regId));
        }

        // UT19 - TEST CASE 3: Invalid Status
        [Fact]
        public async Task DeleteReg_InvalidStatus_ThrowsInvalidOperation()
        {
            // arrange
            int regId = 1;
            int userId = 100;

            var registration = new Registration
            {
                registrationId = regId,
                userId = userId,
                status = "Confirmed" // already Confirmed -> cannot cancel
            };

            _mockUserContext.Setup(u => u.GetCurrentUserId()).Returns(userId);
            _mockRegistrationRepo.Setup(r => r.GetByIdAsync(regId)).ReturnsAsync(registration);

            // act & assert
            await Assert.ThrowsAsync<BusinessRuleException>(() => _service.DeleteRegistrationAsync(regId));
        }

        // UT19 - TEST CASE 4: Success (soft delete -> Canceled & Return True)
        [Fact]
        public async Task DeleteReg_Valid_SetsStatusCancelled_ReturnsTrue()
        {
            // arrange
            int regId = 1;
            int userId = 100;

            var registration = new Registration
            {
                registrationId = regId,
                userId = userId,
                status = "PendingPayment"
            };

            _mockUserContext.Setup(u => u.GetCurrentUserId()).Returns(userId);
            _mockRegistrationRepo.Setup(r => r.GetByIdAsync(regId)).ReturnsAsync(registration);

            _mockRegistrationRepo.Setup(r => r.UpdateAsync(It.IsAny<Registration>())).Returns(Task.CompletedTask);

            // act
            var result = await _service.DeleteRegistrationAsync(regId);

            // assert
            Assert.True(result); 
            Assert.Equal("Canceled", registration.status);

            _mockRegistrationRepo.Verify(r => r.UpdateAsync(It.Is<Registration>(x => x.status == "Canceled")), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        }
    }
}
