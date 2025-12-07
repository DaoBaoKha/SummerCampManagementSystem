namespace SummerCampManagementSystem.BLL.Exceptions
{
    public class RefundPolicyViolationException : BaseException
    {
        public RefundPolicyViolationException(string message) : base(409, message)
        {
        }
    }
}
