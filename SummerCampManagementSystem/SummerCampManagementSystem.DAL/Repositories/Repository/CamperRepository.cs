using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.DAL.Repositories.Repository
{
    public class CamperRepository : GenericRepository<Camper>, ICamperRepository
    {
        public CamperRepository(CampEaseDatabaseContext context) : base(context)
        {
            _context = context;
        }
    }
}
