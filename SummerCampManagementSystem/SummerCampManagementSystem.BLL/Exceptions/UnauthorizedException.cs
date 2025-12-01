namespace SummerCampManagementSystem.BLL.Exceptions
{
    public class UnauthorizedException : BaseException
    {
        public UnauthorizedException(string message) : base(401, message)
        {
        }
    }
}
