
namespace SummerCampManagementSystem.BLL.Helpers
{
    public class ValidationService : IValidationService
    {
        // validates that an entity with the given ID exists using the provided function.
        public async Task ValidateEntityExistsAsync<T>(int id, Func<int, Task<T?>> getByIdFunc, string entityName) where T : class
        {
            if (id <= 0)
                throw new ArgumentException($"{entityName} ID must be greater than 0.");

            var entity = await getByIdFunc(id);
            if (entity == null)
                throw new KeyNotFoundException($"{entityName} with ID {id} not found.");
        }
    }
}
