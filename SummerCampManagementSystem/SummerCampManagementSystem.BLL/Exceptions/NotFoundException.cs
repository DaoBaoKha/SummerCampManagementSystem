namespace SummerCampManagementSystem.BLL.Exceptions
{
    public class NotFoundException : BaseException
    {
        // use when not found id
        public NotFoundException(string message) : base(404, message)
        {
        }
    }
}
