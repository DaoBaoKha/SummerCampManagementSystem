namespace SummerCampManagementSystem.BLL.Exceptions
{
    public class BaseException : Exception
    {
        public int StatusCode { get; }

        public BaseException(int statusCode, string message) : base(message)
        {
            StatusCode = statusCode;
        }
    }
}
