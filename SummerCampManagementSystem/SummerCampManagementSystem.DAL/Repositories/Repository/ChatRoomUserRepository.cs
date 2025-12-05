using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;

namespace SummerCampManagementSystem.DAL.Repositories.Repository
{
    public class ChatRoomUserRepository : GenericRepository<ChatRoomUser>, IChatRoomUserRepository
    {
        public ChatRoomUserRepository(CampEaseDatabaseContext context) : base(context)
        {
        }
    }
}
