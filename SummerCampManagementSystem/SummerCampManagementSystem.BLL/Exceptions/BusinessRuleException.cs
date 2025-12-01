namespace SummerCampManagementSystem.BLL.Exceptions
{
    public class BusinessRuleException : BaseException
    {
        // use when BR errors
        public BusinessRuleException(string message) : base(409, message)
        {
        }
    }
}
