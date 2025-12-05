using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;

namespace SummerCampManagementSystem.DAL.Repositories.Repository
{
    public class MessageRepository : GenericRepository<Message>, IMessageRepository
    {
        public MessageRepository(CampEaseDatabaseContext context) : base(context)
        {
        }
    }
}
