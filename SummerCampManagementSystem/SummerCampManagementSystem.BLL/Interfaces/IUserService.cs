﻿using SummerCampManagementSystem.BLL.DTOs.User;



namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface IUserService
    {
        Task<(AuthResponseDto? authResponse, string? errorMessage, string? errorCode)> LoginAsync(LoginRequestDto model);
        Task<bool> LogoutAsync(int userId);

        Task<RegisterUserResponseDto?> RegisterAsync(RegisterUserRequestDto model);

        Task<VerifyOtpResponseDto?> VerifyOtpAsync(VerifyOtpRequestDto model);
        Task<VerifyOtpResponseDto> ResendActivationOtpAsync(string email);

        Task<ForgotPasswordResponseDto> ForgotPasswordAsync(string email);

        Task<ForgotPasswordResponseDto?> ResetPasswordAsync(ResetPasswordRequestDto model);

        Task<UserResponseDto?> GetUserByIdAsync(int id);

        Task<RegisterUserResponseDto?> CreateStaffAccountAsync(RegisterStaffRequestDto model);
    }
}
