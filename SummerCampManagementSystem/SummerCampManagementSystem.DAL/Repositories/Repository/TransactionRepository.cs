using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;
using System.Transactions;
using Transaction = SummerCampManagementSystem.DAL.Models.Transaction;

namespace SummerCampManagementSystem.DAL.Repositories.Repository
{
    public class TransactionRepository : GenericRepository<Transaction>, ITransactionRepository
    {
        public TransactionRepository(CampEaseDatabaseContext context) : base(context)
        {
        }
    }
}
