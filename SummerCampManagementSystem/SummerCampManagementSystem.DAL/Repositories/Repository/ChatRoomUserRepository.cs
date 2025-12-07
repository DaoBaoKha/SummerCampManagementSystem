using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;

namespace SummerCampManagementSystem.DAL.Repositories.Repository
{
    public class ChatRoomUserRepository : GenericRepository<ChatRoomUser>, IChatRoomUserRepository
    {
        public ChatRoomUserRepository(CampEaseDatabaseContext context) : base(context)
        {
            _context = context;
        }

        public async Task<bool> IsUserInRoomAsync(int userId, int chatRoomId)
        {
            return await _context.ChatRoomUsers
                .AnyAsync(u => u.chatRoomId == chatRoomId && u.userId == userId);
        }

        public async Task<IEnumerable<ChatRoom>> GetRoomsByUserIdAsync(int userId)
        {
            return await _context.ChatRoomUsers
                .Where(cru => cru.userId == userId)
                .Include(cru => cru.chatRoom)
                    .ThenInclude(cr => cr.Messages.OrderByDescending(m => m.createAt).Take(1)) // take one recent message
                        .ThenInclude(m => m.sender) // include sender info
                .Include(cru => cru.chatRoom)
                    .ThenInclude(cr => cr.ChatRoomUsers) // get all users in the chat room
                        .ThenInclude(u => u.user)
                .Select(cru => cru.chatRoom)
                .ToListAsync();
        }
    }
}
