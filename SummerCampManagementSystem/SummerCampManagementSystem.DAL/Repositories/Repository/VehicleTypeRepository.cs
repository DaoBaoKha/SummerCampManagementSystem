﻿using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.DAL.Repositories.Repository
{
    public class VehicleTypeRepository : GenericRepository<VehicleType>, IVehicleTypeRepository
    {
        public VehicleTypeRepository(CampEaseDatabaseContext context) : base(context)
        {
            _context = context;
        }
        public async Task<List<VehicleType>> GetActiveTypesAsync()
        {
            return await _context.VehicleTypes.Where(vt => vt.isActive == true).ToListAsync();
        }
    }
}
