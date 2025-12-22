using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Net.payOS.Types;
using SummerCampManagementSystem.BLL.DTOs.PayOS;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.BLL.Services;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;
using SummerCampManagementSystem.DAL.UnitOfWork;

using TransactionEntity = SummerCampManagementSystem.DAL.Models.Transaction;

namespace SummerCampManagetmentSystem.BLL.Tests.Services
{
    public class PaymentServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IPayOSService> _mockPayOSService;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly PaymentService _service;

        // in memory database context
        private readonly CampEaseDatabaseContext _dbContext;

        // mock repositories to verify updates
        private readonly Mock<ITransactionRepository> _mockTransRepo;
        private readonly Mock<IRegistrationRepository> _mockRegRepo;
        private readonly Mock<IRegistrationCamperRepository> _mockRegCamperRepo;
        private readonly Mock<IRegistrationOptionalActivityRepository> _mockRegOptRepo;
        private readonly Mock<ICamperActivityRepository> _mockCamperActRepo;
        private readonly Mock<IGroupRepository> _mockGroupRepo;
        private readonly Mock<IAccommodationRepository> _mockAccomRepo;
        private readonly Mock<ICamperGroupRepository> _mockCamperGroupRepo;
        private readonly Mock<ICamperAccommodationRepository> _mockCamperAccomRepo;
        private readonly Mock<ICamperRepository> _mockCamperRepo;

        public PaymentServiceTests()
        {
            // setup InMemory Database
            var options = new DbContextOptionsBuilder<CampEaseDatabaseContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _dbContext = new CampEaseDatabaseContext(options);

            // init Mocks
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockPayOSService = new Mock<IPayOSService>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockConfiguration.Setup(c => c["PayOS:ChecksumKey"]).Returns("dummyKey");

            // init Mock Repos
            _mockTransRepo = new Mock<ITransactionRepository>();
            _mockRegRepo = new Mock<IRegistrationRepository>();
            _mockRegCamperRepo = new Mock<IRegistrationCamperRepository>();
            _mockRegOptRepo = new Mock<IRegistrationOptionalActivityRepository>();
            _mockCamperActRepo = new Mock<ICamperActivityRepository>();
            _mockGroupRepo = new Mock<IGroupRepository>();
            _mockAccomRepo = new Mock<IAccommodationRepository>();
            _mockCamperGroupRepo = new Mock<ICamperGroupRepository>();
            _mockCamperAccomRepo = new Mock<ICamperAccommodationRepository>();
            _mockCamperRepo = new Mock<ICamperRepository>();

            // setup UnitOfWork to return mock repos
            _mockUnitOfWork.Setup(u => u.Transactions).Returns(_mockTransRepo.Object);
            _mockTransRepo.Setup(r => r.GetQueryable()).Returns(_dbContext.Transactions);

            _mockUnitOfWork.Setup(u => u.Registrations).Returns(_mockRegRepo.Object);
            _mockRegRepo.Setup(r => r.GetQueryable()).Returns(_dbContext.Registrations);

            _mockUnitOfWork.Setup(u => u.RegistrationCampers).Returns(_mockRegCamperRepo.Object);
            // _mockRegCamperRepo.Setup(r => r.GetQueryable()).Returns(_dbContext.RegistrationCampers); 

            _mockUnitOfWork.Setup(u => u.RegistrationOptionalActivities).Returns(_mockRegOptRepo.Object);
            _mockRegOptRepo.Setup(r => r.GetQueryable()).Returns(_dbContext.RegistrationOptionalActivities);

            _mockUnitOfWork.Setup(u => u.CamperActivities).Returns(_mockCamperActRepo.Object);
            _mockCamperActRepo.Setup(r => r.GetQueryable()).Returns(_dbContext.CamperActivities);

            _mockUnitOfWork.Setup(u => u.Groups).Returns(_mockGroupRepo.Object);
            _mockGroupRepo.Setup(r => r.GetQueryable()).Returns(_dbContext.Groups);

            _mockUnitOfWork.Setup(u => u.Accommodations).Returns(_mockAccomRepo.Object);
            _mockAccomRepo.Setup(r => r.GetQueryable()).Returns(_dbContext.Accommodations);

            _mockUnitOfWork.Setup(u => u.Campers).Returns(_mockCamperRepo.Object);
            _mockCamperRepo.Setup(r => r.GetQueryable()).Returns(_dbContext.Campers);

            _mockUnitOfWork.Setup(u => u.CamperGroups).Returns(_mockCamperGroupRepo.Object);
            _mockUnitOfWork.Setup(u => u.CamperAccommodations).Returns(_mockCamperAccomRepo.Object);

            // mock DbContext retrieval
            _mockUnitOfWork.Setup(u => u.GetDbContext()).Returns(_dbContext);

            _service = new PaymentService(
                _mockUnitOfWork.Object,
                _mockPayOSService.Object,
                _mockConfiguration.Object
            );
        }

        [Fact]
        public async Task HandleWebhook_TransactionNotFound_Ignores()
        {
            var webhookRequest = new PayOSWebhookRequestDto
            {
                data = new PayOSWebhookDataDto { orderCode = 12345, amount = 1000 },
                code = "00",
                success = true
            };
            var verifiedData = new WebhookData(12345, 1000, "desc", null, null, null, null, null, "00", "Success", null, null, null, null, null, null);
            _mockPayOSService.Setup(p => p.VerifyPaymentWebhookData(It.IsAny<WebhookType>())).Returns(verifiedData);

            await _service.HandlePayOSWebhook(webhookRequest);
            _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Never);
        }

        [Fact]
        public async Task HandleWebhook_RegistrationNotFound_Ignores()
        {
            var webhookRequest = new PayOSWebhookRequestDto { data = new PayOSWebhookDataDto { orderCode = 123 }, code = "00" };
            var verifiedData = new WebhookData(123, 1000, "desc", null, null, null, null, null, "00", "Success", null, null, null, null, null, null);
            _mockPayOSService.Setup(p => p.VerifyPaymentWebhookData(It.IsAny<WebhookType>())).Returns(verifiedData);

            _dbContext.Transactions.Add(new TransactionEntity { transactionCode = "123", status = "Pending", registrationId = 99 });
            await _dbContext.SaveChangesAsync();

            await _service.HandlePayOSWebhook(webhookRequest);
            _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Never);
        }

        [Fact]
        public async Task HandleWebhook_PaymentFailed_UpdatesTransactionToFailed()
        {
            var webhookRequest = new PayOSWebhookRequestDto { data = new PayOSWebhookDataDto { orderCode = 123 }, code = "01" };
            var verifiedData = new WebhookData(123, 1000, "desc", null, null, null, null, null, "01", "Fail", null, null, null, null, null, null);
            _mockPayOSService.Setup(p => p.VerifyPaymentWebhookData(It.IsAny<WebhookType>())).Returns(verifiedData);

            var trans = new TransactionEntity { transactionId = 1, transactionCode = "123", status = "Pending", registrationId = 1 };
            _dbContext.Transactions.Add(trans);
            
            // Add registration to avoid NullReferenceException
            _dbContext.Registrations.Add(new Registration { registrationId = 1, status = "PendingPayment" });
            
            await _dbContext.SaveChangesAsync();

            _mockTransRepo.Setup(r => r.UpdateAsync(It.IsAny<TransactionEntity>()))
                .Callback<TransactionEntity>(t => { })
                .Returns(Task.CompletedTask);

            await _service.HandlePayOSWebhook(webhookRequest);

            _mockTransRepo.Verify(r => r.UpdateAsync(It.Is<TransactionEntity>(t => t.status == "Failed")), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task HandleWebhook_PaymentSuccess_UpdatesStatusAndAssigns()
        {
            var webhookRequest = new PayOSWebhookRequestDto { data = new PayOSWebhookDataDto { orderCode = 123 }, code = "00", success = true };
            var verifiedData = new WebhookData(123, 1000, "desc", null, null, null, null, null, "00", "Success", null, null, null, null, null, null);
            _mockPayOSService.Setup(p => p.VerifyPaymentWebhookData(It.IsAny<WebhookType>())).Returns(verifiedData);

            // data setup
            var trans = new TransactionEntity { transactionId = 1, transactionCode = "123", status = "Pending", registrationId = 1 };
            var reg = new Registration { registrationId = 1, status = "PendingPayment", campId = 1 };

            var camper1 = new RegistrationCamper
            {
                registrationId = 1, // composite key part 1
                camperId = 101,     // composite key part 2
                status = "Approved"
            };
            // -------------------------------------------------------------

            var camperInfo = new Camper { camperId = 101, dob = DateOnly.FromDateTime(DateTime.Now.AddYears(-10)) };
            var group = new Group { groupId = 10, campId = 1, minAge = 8, maxAge = 12, maxSize = 20 };
            var accom = new Accommodation { accommodationId = 5, campId = 1, capacity = 10, isActive = true };

            // add to InMemory DB
            _dbContext.Transactions.Add(trans);
            _dbContext.Registrations.Add(reg);
            _dbContext.RegistrationCampers.Add(camper1);
            _dbContext.Campers.Add(camperInfo);
            _dbContext.Groups.Add(group);
            _dbContext.Accommodations.Add(accom);
            await _dbContext.SaveChangesAsync();

            // mock setup
            _mockTransRepo.Setup(r => r.UpdateAsync(It.IsAny<TransactionEntity>())).Returns(Task.CompletedTask);
            _mockRegRepo.Setup(r => r.UpdateAsync(It.IsAny<Registration>())).Returns(Task.CompletedTask);
            _mockRegCamperRepo.Setup(r => r.UpdateAsync(It.IsAny<RegistrationCamper>())).Returns(Task.CompletedTask);
            _mockCamperGroupRepo.Setup(r => r.AddRangeAsync(It.IsAny<List<CamperGroup>>())).Returns(Task.CompletedTask);
            _mockCamperAccomRepo.Setup(r => r.AddRangeAsync(It.IsAny<List<CamperAccommodation>>())).Returns(Task.CompletedTask);

            // act
            await _service.HandlePayOSWebhook(webhookRequest);

            // assert
            _mockTransRepo.Verify(r => r.UpdateAsync(It.Is<TransactionEntity>(t => t.status == "Confirmed")), Times.Once);
            _mockRegRepo.Verify(r => r.UpdateAsync(It.Is<Registration>(r => r.status == "Confirmed")), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        }
    }
}