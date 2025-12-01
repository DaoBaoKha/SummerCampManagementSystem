using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using SummerCampManagementSystem.BLL.DTOs.Driver;
using SummerCampManagementSystem.BLL.Exceptions;
using SummerCampManagementSystem.BLL.Helpers;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.Core.Enums;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;

namespace SummerCampManagementSystem.BLL.Services
{
    public class DriverService : IDriverService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IUserService _userService;
        private readonly IUploadSupabaseService _supabaseService;
        private readonly IConfiguration _config;
        private readonly IEmailService _emailService; 
        private readonly IMemoryCache _cache; 
        private readonly IUserContextService _userContextService;

        public DriverService(IUnitOfWork unitOfWork, IMapper mapper, IUserService userService,
            IUploadSupabaseService supabaseService, IConfiguration configuration, IEmailService emailService,
            IMemoryCache cache, IUserContextService userContextService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _userService = userService;
            _supabaseService = supabaseService;
            _config = configuration;
            _emailService = emailService;
            _cache = cache;
            _userContextService = userContextService;
        }

        public async Task<DriverRegisterResponseDto> RegisterDriverAsync(DriverRegisterDto model)
        {
            var existingUser = await _unitOfWork.Users.GetQueryable().AnyAsync(u => u.email == model.Email);
            if (existingUser)
            {
                throw new ArgumentException("Địa chỉ email đã tồn tại trong hệ thống.");
            }

            string defaultAvatar = _config["AppSetting:DefaultAvatarUrl"]
                                   ?? "https://via.placeholder.com/150";

            var newUserAccount = new UserAccount
            {
                firstName = model.FirstName,
                lastName = model.LastName,
                email = model.Email,
                phoneNumber = model.PhoneNumber,
                password = _userService.HashPassword(model.Password), 
                avatar = defaultAvatar,
                dob = model.Dob,
                role = UserRole.Driver.ToString(),
                isActive = false,
                createAt = DateTime.UtcNow
            };

            await _unitOfWork.Users.CreateAsync(newUserAccount);
            await _unitOfWork.CommitAsync();

            var newUserId = newUserAccount.userId;

            var driverEntity = _mapper.Map<Driver>(model);
            driverEntity.userId = newUserId;
            driverEntity.status = DriverStatus.PendingUpload.ToString();

            // create one time token
            var uploadToken = Guid.NewGuid().ToString("N"); // random string

            driverEntity.UploadToken = uploadToken;
            driverEntity.TokenExpiry = DateTime.UtcNow.AddMinutes(15); 
            driverEntity.IsTokenUsed = false;

            await _unitOfWork.Drivers.CreateAsync(driverEntity);
            await _unitOfWork.CommitAsync();

            var otp = new Random().Next(100000, 999999).ToString();

            string normalizedEmail = model.Email.Trim().ToLower();

            _cache.Set($"OTP_Activation_{normalizedEmail}", otp, TimeSpan.FromMinutes(5));

            if (string.IsNullOrWhiteSpace(model.Email) || !model.Email.Contains("@"))
                throw new ArgumentException($"Địa chỉ Email không chính xác: {model.Email}");

            await _emailService.SendOtpEmailAsync(model.Email, otp, "Activation");

            return new DriverRegisterResponseDto
            {
                UserId = newUserId,
                Message = "Đăng ký thành công! Mã OTP đã được gửi tới email của bạn để kích hoạt tài khoản.",
                OneTimeUploadToken = uploadToken, 
                DriverDetails = _mapper.Map<DriverDetailsDto>(driverEntity)
            };
        }


        public async Task<DriverResponseDto> GetDriverByUserIdAsync(int userId)
        {
            var driver = await _unitOfWork.Drivers.GetQueryable()
                                                  .Where(d => d.userId == userId)
                                                  .Include(d => d.user)
                                                  .FirstOrDefaultAsync()
                                                  ?? throw new KeyNotFoundException($"Không tìm thấy thông tin tài xế với UserId = {userId}.");

            return _mapper.Map<DriverResponseDto>(driver);
        }

        public async Task<IEnumerable<DriverResponseDto>> GetAllDriversAsync()
        {
            var drivers = await _unitOfWork.Drivers.GetQueryable()
                                                  .Include(d => d.user)
                                                  .ToListAsync();

            return _mapper.Map<IEnumerable<DriverResponseDto>>(drivers);
        }


        public async Task<IEnumerable<DriverResponseDto>> GetDriverByStatusAsync(string status)
        {
            var drivers = await _unitOfWork.Drivers.GetQueryable()
                                                  .Where(d => d.status == status)
                                                  .Include(d => d.user)
                                                  .ToListAsync();
            return _mapper.Map<IEnumerable<DriverResponseDto>>(drivers);
        }

        public async Task<DriverResponseDto> UpdateDriverAsync(int driverId, DriverRequestDto driverRequestDto)
        {
            var driverToUpdate = await _unitOfWork.Drivers.GetByIdAsync(driverId)
                ?? throw new KeyNotFoundException($"Không tìm thấy Driver với DriverId = {driverId} để cập nhật.");

            _mapper.Map(driverRequestDto, driverToUpdate);


            await _unitOfWork.Drivers.UpdateAsync(driverToUpdate);
            await _unitOfWork.CommitAsync();

            var updatedDriver = await _unitOfWork.Drivers.GetQueryable()
                                                         .Where(d => d.driverId == driverId)
                                                         .Include(d => d.user)
                                                         .FirstAsync();

            updatedDriver.status = DriverStatus.PendingApproval.ToString();

            return _mapper.Map<DriverResponseDto>(updatedDriver);
        }


        public async Task<bool> DeleteDriverAsync(int driverId)
        {
            var driverToDelete = await _unitOfWork.Drivers.GetByIdAsync(driverId)
                ?? throw new KeyNotFoundException($"Không tìm thấy Driver với DriverId = {driverId} để xóa.");

            await _unitOfWork.Drivers.RemoveAsync(driverToDelete);
            await _unitOfWork.CommitAsync();

            return true;
        }

        public async Task<string> UpdateDriverAvatarAsync(int userId, IFormFile file)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId)
                ?? throw new KeyNotFoundException($"User with ID {userId} not found.");

            var avatarUrl = await _supabaseService.UploadDriverAvatarAsync(userId, file);

            if (string.IsNullOrEmpty(avatarUrl))
            {
                throw new Exception("Upload thất bại. Không thể lấy được URL ảnh.");
            }

            user.avatar = avatarUrl;

            await _unitOfWork.Users.UpdateAsync(user);
            await _unitOfWork.CommitAsync();

            return avatarUrl;
        }

        public async Task<string> UpdateDriverLicensePhotoAsync(IFormFile file)
        {
            var userId = _userContextService.GetCurrentUserId()
                ?? throw new InvalidOperationException("Không thể lấy thông tin người dùng từ ngữ cảnh hiện tại.");

            var driver = await _unitOfWork.Drivers.GetQueryable().FirstOrDefaultAsync(d => d.userId == userId)
                ?? throw new KeyNotFoundException($"Không tìm thấy thông tin tài xế với UserId = {userId}.");

            var licensePhotoUrl = await _supabaseService.UploadDriverLicensePhotoAsync(userId, file);

            if (string.IsNullOrEmpty(licensePhotoUrl))
            {
                throw new Exception("Upload thất bại. Không thể lấy được URL ảnh giấy phép lái xe.");
            }

            driver.licensePhoto = licensePhotoUrl;
            driver.status = DriverStatus.PendingApproval.ToString();

            await _unitOfWork.Drivers.UpdateAsync(driver);
            await _unitOfWork.CommitAsync();

            return licensePhotoUrl;
        }

        public async Task<DriverResponseDto> UpdateDriverStatusAsync(int driverId, DriverStatusUpdateDto updateDto)
        {
            var driver = await _unitOfWork.Drivers.GetByIdAsync(driverId)
                ?? throw new KeyNotFoundException($"Không tìm thấy Driver với ID {driverId}.");

            DriverStatus newStatus = updateDto.Status;
            string pendingApprovalStatus = DriverStatus.PendingApproval.ToString();

            // status validation
            if (newStatus != DriverStatus.Approved && newStatus != DriverStatus.Rejected)
            {
                throw new ArgumentException("Trạng thái mới phải là Approved hoặc Rejected.");
            }

            // only update when status = pendingApproval
            if (driver.status != pendingApprovalStatus)
            {
                throw new InvalidOperationException($"Chỉ có thể duyệt/từ chối Driver ở trạng thái '{pendingApprovalStatus}'. Trạng thái hiện tại: {driver.status}.");
            }

            driver.status = newStatus.ToString();

            await _unitOfWork.Drivers.UpdateAsync(driver);
            await _unitOfWork.CommitAsync();

            var updatedDriver = await GetDriverByUserIdAsync(driver.userId.Value);

            return updatedDriver;
        }

        public async Task<string> UpdateDriverLicensePhotoByTokenAsync(string uploadToken, IFormFile file)
        {
            // find driver by token
            var driver = await _unitOfWork.Drivers.GetQueryable()
                .FirstOrDefaultAsync(d => d.UploadToken == uploadToken)
                ?? throw new KeyNotFoundException("Token upload ảnh không hợp lệ hoặc không tồn tại.");

            // validate token
            if (driver.IsTokenUsed == true)
            {
                throw new InvalidOperationException("Token upload ảnh đã được sử dụng.");
            }

            if (driver.TokenExpiry.HasValue && driver.TokenExpiry.Value < DateTime.UtcNow)
            {
                // delete expired token
                driver.UploadToken = null;
                await _unitOfWork.Drivers.UpdateAsync(driver);
                await _unitOfWork.CommitAsync();
                throw new InvalidOperationException("Token upload ảnh đã hết hạn.");
            }

            var userId = driver.userId
                ?? throw new InvalidOperationException("Driver entity thiếu liên kết người dùng.");

            // upload photo
            var licensePhotoUrl = await _supabaseService.UploadDriverLicensePhotoAsync(userId, file);

            if (string.IsNullOrEmpty(licensePhotoUrl))
            {
                throw new Exception("Upload thất bại. Không thể lấy được URL ảnh giấy phép lái xe.");
            }

            // destroy token and update status
            driver.licensePhoto = licensePhotoUrl;
            driver.status = DriverStatus.PendingApproval.ToString();

            driver.IsTokenUsed = true; // mark token as used
            driver.UploadToken = null; // remove from db 

            await _unitOfWork.Drivers.UpdateAsync(driver);
            await _unitOfWork.CommitAsync();

            return licensePhotoUrl;
        }

        public async Task<IEnumerable<DriverResponseDto>> GetAvailableDriversAsync(DateOnly? date, TimeOnly? startTime, TimeOnly? endTime)
        {
            // if date = null -> get current date
            var checkDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);

            // time validation
            if (startTime.HasValue != endTime.HasValue)
            {
                throw new BusinessRuleException("Phải nhập cả thời gian bắt đầu (startTime) và thời gian kết thúc (endTime) nếu nhập một trong hai.");
            }

            // if time = null -> get all time from current date
            var checkStartTime = startTime ?? new TimeOnly(0, 0, 0); // 00:00:00
            var checkEndTime = endTime ?? new TimeOnly(23, 59, 59); // 23:59:59

            if (checkStartTime >= checkEndTime)
            {
                throw new BusinessRuleException("Thời gian bắt đầu phải sớm hơn thời gian kết thúc.");
            }

            if (checkDate < DateOnly.FromDateTime(DateTime.UtcNow.Date))
            {
                throw new BusinessRuleException("Không thể tìm tài xế cho ngày đã qua.");
            }

            // status to check conflict
            var activeScheduleStatuses = new[]
            {
                TransportScheduleStatus.Draft.ToString(),
                TransportScheduleStatus.NotYet.ToString(),
                TransportScheduleStatus.InProgress.ToString()
            };

            // find all unavailable driver
            var conflictingDriverIds = await _unitOfWork.TransportSchedules.GetQueryable()
                .Where(s => s.date == checkDate && activeScheduleStatuses.Contains(s.status))

                // conflict logic: 
                // current schedule ends right after searching (checkStartTime)
                // current schedule starts right before searching (checkEndTime)
                .Where(s => s.endTime > checkStartTime && s.startTime < checkEndTime)
                .Where(s => s.driverId.HasValue) // only schedule with driverId
                .Select(s => s.driverId.Value)
                .Distinct()
                .ToListAsync();

            // find all approved driver
            var allApprovedDrivers = _unitOfWork.Drivers.GetQueryable()
                .Where(d => d.status == DriverStatus.Approved.ToString())
                .Include(d => d.user);

            // exclude conflict driver
            var availableDrivers = await allApprovedDrivers
                .Where(d => !conflictingDriverIds.Contains(d.driverId))
                .ToListAsync();

            if (!availableDrivers.Any())
            {
                return Enumerable.Empty<DriverResponseDto>();
            }

            return _mapper.Map<IEnumerable<DriverResponseDto>>(availableDrivers);
        }
    }
}