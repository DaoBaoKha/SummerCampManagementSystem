using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.DTOs.Feedback
{
    public class FeedbackResponseDto
    {
        public int FeedbackId { get; set; }

        public int RegistrationId { get; set; }

        public int? Rating { get; set; }

        public string? Comment { get; set; }

        public DateTime? CreateAt { get; set; }

        public DateTime? UpdateAt { get; set; }

        public string? Status { get; set; }

        public string? RejectionReason { get; set; }

        public string? ManagerReply { get; set; }

        public DateTime? ReplyAt { get; set; }
    }
}
