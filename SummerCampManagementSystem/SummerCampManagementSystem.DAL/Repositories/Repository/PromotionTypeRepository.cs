using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.DAL.Repositories.Repository
{
    public class PromotionTypeRepository : GenericRepository<PromotionType>, IPromotionTypeRepository
    {
        public PromotionTypeRepository(CampEaseDatabaseContext context) : base(context)
        {
        }
    }
}
