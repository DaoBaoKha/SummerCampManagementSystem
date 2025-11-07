using AutoMapper;
using SummerCampManagementSystem.BLL.DTOs.Accommodation;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;
using SummerCampManagementSystem.DAL.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.Services
{
    public class AccommodationService : IAccommodationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public AccommodationService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<AccommodationResponseDto>> GetAllBySupervisorIdAsync(int supervisorId)
        {
            var accommodations = await _unitOfWork.Accommodations.GetAllBySupervisorIdAsync(supervisorId);
            return _mapper.Map<IEnumerable<AccommodationResponseDto>>(accommodations);
        }
    }
}
