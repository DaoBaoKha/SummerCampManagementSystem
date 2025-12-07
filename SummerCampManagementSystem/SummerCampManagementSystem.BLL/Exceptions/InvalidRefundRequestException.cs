namespace SummerCampManagementSystem.BLL.Exceptions
{
    public class InvalidRefundRequestException : BaseException
    {
        public InvalidRefundRequestException(string message) : base(400, message)
        {
        }
    }
}
