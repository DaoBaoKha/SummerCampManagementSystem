using SummerCampManagementSystem.DAL.Models;

namespace SummerCampManagementSystem.DAL.Repositories.Interfaces
{
    public interface IChatRoomUserRepository : IGenericRepository<ChatRoomUser>
    {
        Task<bool> IsUserInRoomAsync(int userId, int chatRoomId);
        Task<IEnumerable<ChatRoom>> GetRoomsByUserIdAsync(int userId);

    }
}
