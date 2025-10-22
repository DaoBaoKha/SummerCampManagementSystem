namespace SummerCampManagementSystem.BLL.DTOs.Shared
{
    public class SuccessResponseDto<T>
    {
        public string Message { get; set; }
        public T Data { get; set; }
    }

    public class MessageResponseDto
    {
        public string Message { get; set; }
    }
}
