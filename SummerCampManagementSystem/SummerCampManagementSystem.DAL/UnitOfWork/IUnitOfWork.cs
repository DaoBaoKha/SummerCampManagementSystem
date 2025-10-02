using SummerCampManagementSystem.DAL.Repositories.UserRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.DAL.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
       IUserRepository Users { get; }
       Task<int> CommitAsync();
    }
}
