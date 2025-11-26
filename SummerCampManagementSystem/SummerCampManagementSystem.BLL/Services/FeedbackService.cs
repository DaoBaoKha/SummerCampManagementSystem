using AutoMapper;
using SummerCampManagementSystem.BLL.DTOs.Feedback;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.Services
{
    public class FeedbackService : IFeedbackService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public FeedbackService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<FeedbackResponseDto> CreateFeedbackAsync(FeedbackRequestDto dto)
        {
            var registration = await _unitOfWork.Registrations.GetByIdAsync(dto.RegistrationId)
                ?? throw new KeyNotFoundException($"Registration with id {dto.RegistrationId} not found");

            var feedback = _mapper.Map<Feedback>(dto);
            feedback.createAt = DateTime.UtcNow;
            await _unitOfWork.Feedbacks.CreateAsync(feedback);
            await _unitOfWork.CommitAsync();
            return _mapper.Map<FeedbackResponseDto>(feedback);

        }

        public async Task<bool> DeleteFeedbackAsync(int feedbackId)
        {
            var feedback = await _unitOfWork.Feedbacks.GetByIdAsync(feedbackId);

            if (feedback == null) return false;
            await _unitOfWork.Feedbacks.RemoveAsync(feedback);
            await _unitOfWork.CommitAsync();
            return true;
        }

        public async Task<IEnumerable<FeedbackResponseDto>> GetAllFeedbacksAsync()
        {
            var feedbacks = await _unitOfWork.Feedbacks.GetAllAsync();
            return _mapper.Map<IEnumerable<FeedbackResponseDto>>(feedbacks);
        }

        public async Task<FeedbackResponseDto?> GetFeedbackByIdAsync(int feedbackId)
        {
            var feedback = await _unitOfWork.Feedbacks.GetByIdAsync(feedbackId);
            return feedback == null ? null : _mapper.Map<FeedbackResponseDto>(feedback);
        }

        public async Task<FeedbackResponseDto?> UpdateFeedbackAsync(int feedbackId, FeedbackRequestDto dto)
        {
            var feedback = await _unitOfWork.Feedbacks.GetByIdAsync(feedbackId);
            if(feedback == null) return null;

            _mapper.Map(dto, feedback);
            feedback.updateAt = DateTime.UtcNow;
            await _unitOfWork.Feedbacks.UpdateAsync(feedback);
            await _unitOfWork.CommitAsync();
            return _mapper.Map<FeedbackResponseDto>(feedback);
        }

        public async Task<FeedbackResponseDto> ReplyFeedback(int feedbackId, FeedbackReplyRequestDto dto)
        {           
            var feedback = await _unitOfWork.Feedbacks.GetByIdAsync(feedbackId)
                ?? throw new KeyNotFoundException($"Feedback with id {feedbackId} not found");

            feedback.managerReply = dto.Reply;
            feedback.replyAt = DateTime.UtcNow;
            feedback.status = "Replied";
            await _unitOfWork.Feedbacks.UpdateAsync(feedback);
            await _unitOfWork.CommitAsync();
            return _mapper.Map<FeedbackResponseDto>(feedback);
        }
    }
}
