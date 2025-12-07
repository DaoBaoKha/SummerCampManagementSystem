using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;

namespace SummerCampManagementSystem.DAL.Repositories.Repository
{
    public class MessageRepository : GenericRepository<Message>, IMessageRepository
    {
        public MessageRepository(CampEaseDatabaseContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Message>> GetMessagesByRoomIdAsync(int chatRoomId)
        {
            return await _context.Messages
                .Where(m => m.chatRoomId == chatRoomId)
                .Include(m => m.sender) 
                .OrderBy(m => m.createAt)
                .ToListAsync();
        }
    }
}
