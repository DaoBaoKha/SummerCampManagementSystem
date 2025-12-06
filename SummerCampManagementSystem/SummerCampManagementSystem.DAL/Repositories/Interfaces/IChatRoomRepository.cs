using SummerCampManagementSystem.DAL.Models;

namespace SummerCampManagementSystem.DAL.Repositories.Interfaces
{
    public interface IChatRoomRepository : IGenericRepository<ChatRoom>
    {
        Task<bool> ExistsAsync(int chatRoomId);
    }
}
