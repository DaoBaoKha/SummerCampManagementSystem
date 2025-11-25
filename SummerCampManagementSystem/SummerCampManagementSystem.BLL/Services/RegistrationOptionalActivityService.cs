using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.BLL.DTOs.RegistrationOptionalActivity;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;

namespace SummerCampManagementSystem.BLL.Services
{
    public class RegistrationOptionalActivityService : IRegistrationOptionalActivityService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly CampEaseDatabaseContext _context;

        public RegistrationOptionalActivityService(IUnitOfWork unitOfWork, IMapper mapper, CampEaseDatabaseContext context)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _context = context;
        }

        public async Task<IEnumerable<RegistrationOptionalActivityResponseDto>> GetAllAsync()
        {
            var entities = await _unitOfWork.RegistrationOptionalActivities.GetAllAsync();

            return _mapper.Map<IEnumerable<RegistrationOptionalActivityResponseDto>>(entities);
        }

        public async Task<RegistrationOptionalActivityResponseDto> GetByIdAsync(int id)
        {
            var entity = await _unitOfWork.RegistrationOptionalActivities.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"RegistrationOptionalActivity with ID {id} not found.");

            return _mapper.Map<RegistrationOptionalActivityResponseDto>(entity);
        }

        public async Task<IEnumerable<RegistrationOptionalActivityResponseDto>> SearchAsync(RegistrationOptionalActivitySearchDto searchDto)
        {
            // queryable from DbContext
            IQueryable<RegistrationOptionalActivity> query = _context.RegistrationOptionalActivities;

            if (searchDto.RegistrationId.HasValue && searchDto.RegistrationId.Value > 0)
            {
                query = query.Where(r => r.registrationId == searchDto.RegistrationId.Value);
            }

            if (searchDto.CamperId.HasValue && searchDto.CamperId.Value > 0)
            {
                query = query.Where(r => r.camperId == searchDto.CamperId.Value);
            }

            if (searchDto.ActivityScheduleId.HasValue && searchDto.ActivityScheduleId.Value > 0)
            {
                query = query.Where(r => r.activityScheduleId == searchDto.ActivityScheduleId.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchDto.Status))
            {
                string statusLower = searchDto.Status.Trim().ToLower();

                // use toLower for case-insensitive comparison
                query = query.Where(r => r.status.ToLower().Contains(statusLower));
            }

            var entities = await query.ToListAsync();

            return _mapper.Map<IEnumerable<RegistrationOptionalActivityResponseDto>>(entities);
        }
    }
}
