using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.DTOs.Feedback
{
    public class FeedbackRequestDto
    {
        public int RegistrationId { get; set; }

        public int? Rating { get; set; }

        public string? Comment { get; set; }
    }

    public class FeedbackReplyRequestDto
    {
        public string Reply { get; set; } = null!;
    }

    public class FeedbackRejectedRequestDto
    {
        [Required]
        public string RejectionReason { get; set; } = null!;
    }
}
