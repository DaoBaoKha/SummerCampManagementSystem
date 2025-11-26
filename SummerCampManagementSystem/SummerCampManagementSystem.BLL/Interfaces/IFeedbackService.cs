using SummerCampManagementSystem.BLL.DTOs.Feedback;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface IFeedbackService
    {
        Task <FeedbackResponseDto> CreateFeedbackAsync(FeedbackRequestDto dto);
        Task<FeedbackResponseDto?> GetFeedbackByIdAsync(int feedbackId);
        Task<IEnumerable<FeedbackResponseDto>> GetAllFeedbacksAsync();
        Task<FeedbackResponseDto?> UpdateFeedbackAsync(int feedbackId, FeedbackRequestDto dto);
        Task<bool> DeleteFeedbackAsync(int feedbackId);
        Task<FeedbackResponseDto> ReplyFeedback(int feedbackId, FeedbackReplyRequestDto dto);
    }
}
