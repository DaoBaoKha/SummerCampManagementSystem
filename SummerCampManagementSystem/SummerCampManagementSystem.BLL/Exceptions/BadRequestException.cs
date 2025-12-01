namespace SummerCampManagementSystem.BLL.Exceptions
{
    public class BadRequestException : BaseException
    {
        // use when wrong request, invalid data
        public BadRequestException(string message) : base(400, message)
        {
        }
    }
}
