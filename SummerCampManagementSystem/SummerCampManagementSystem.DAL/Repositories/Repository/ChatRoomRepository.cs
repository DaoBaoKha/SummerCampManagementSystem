using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;

namespace SummerCampManagementSystem.DAL.Repositories.Repository
{
    public class ChatRoomRepository : GenericRepository<ChatRoom>, IChatRoomRepository
    {
        public ChatRoomRepository(CampEaseDatabaseContext context) : base(context)
        {
            _context = context;
        }

        public async Task<bool> ExistsAsync(int chatRoomId)
        {
            return await _context.ChatRooms.AnyAsync(r => r.chatRoomId == chatRoomId);
        }
    }
}
