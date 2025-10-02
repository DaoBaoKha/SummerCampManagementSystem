using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.UserRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.DAL.UnitOfWork
{
    //public class UnitOfWork : IUnitOfWork
    //{
    //    private readonly CampEaseDatabaseContext _context;
    //    public IUserRepository Users { get; }

    //    public UnitOfWork(CampEaseDatabaseContext context, IUserRepository userRepository)
    //    {
    //        _context = context;
    //        Users = userRepository;
    //    }

    //    public async Task<int> CommitAsync()
    //    {
    //        return await _context.SaveChangesAsync();
    //    }

    //    public void Dispose()
    //    {
    //        _context.Dispose();
    //    }
    //}
}
