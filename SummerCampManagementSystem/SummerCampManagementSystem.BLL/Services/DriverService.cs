using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.BLL.DTOs.Driver;
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

        public DriverService(IUnitOfWork unitOfWork, IMapper mapper, IUserService userService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _userService = userService;
        }

        public async Task<DriverRegisterResponseDto> RegisterDriverAsync(DriverRegisterDto model)
        {
            var existingUser = await _unitOfWork.Users.GetQueryable().AnyAsync(u => u.email == model.Email);
            if (existingUser)
            {
                throw new ArgumentException("Địa chỉ email đã tồn tại trong hệ thống.");
            }

            var newUserAccount = new UserAccount
            {
                firstName = model.FirstName,
                lastName = model.LastName,
                email = model.Email,
                phoneNumber = model.PhoneNumber,
                password = _userService.HashPassword(model.Password), 
                dob = model.Dob,
                role = UserRole.Driver.ToString(),
                isActive = true,
                createAt = DateTime.UtcNow
            };

            await _unitOfWork.Users.CreateAsync(newUserAccount);
            await _unitOfWork.CommitAsync();

            var newUserId = newUserAccount.userId;

            var driverEntity = _mapper.Map<Driver>(model);
            driverEntity.userId = newUserId;

            await _unitOfWork.Drivers.CreateAsync(driverEntity);
            await _unitOfWork.CommitAsync(); 

            return new DriverRegisterResponseDto
            {
                UserId = newUserId,
                Message = $"Đăng ký tài xế thành công! Tài khoản {newUserAccount.email} đã được tạo.",
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
    }
}