using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;

namespace SummerCampManagementSystem.DAL.Repositories.Repository
{
    public class ChatRoomRepository : GenericRepository<ChatRoom>, IChatRoomRepository
    {
        public ChatRoomRepository(CampEaseDatabaseContext context) : base(context)
        {
        }
    }
}
