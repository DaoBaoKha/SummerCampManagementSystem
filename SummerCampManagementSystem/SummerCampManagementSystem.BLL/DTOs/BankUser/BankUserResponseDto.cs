namespace SummerCampManagementSystem.BLL.DTOs.BankUser
{
    public class BankUserResponseDto
    {
        public int BankUserId { get; set; }
        public int UserId { get; set; }
        public string BankCode { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string BankNumber { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public bool IsActive { get; set; } 
    }
}
