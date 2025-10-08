namespace SummerCampManagementSystem.BLL.Helpers
{
    public interface IValidationService
    {
        Task ValidateEntityExistsAsync<T>(int id, Func<int, Task<T?>> getByIdFunc, string entityName) where T : class;
    }

}
