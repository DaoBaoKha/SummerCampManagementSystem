using AutoMapper;
using SummerCampManagementSystem.BLL.DTOs.Requests.Camper;
using SummerCampManagementSystem.BLL.DTOs.Requests.Guardian;
using SummerCampManagementSystem.BLL.DTOs.Responses.Camp;
using SummerCampManagementSystem.BLL.DTOs.Responses.Camper;
using SummerCampManagementSystem.BLL.DTOs.Responses.Guardian;
using SummerCampManagementSystem.BLL.DTOs.Responses.Registration;
using SummerCampManagementSystem.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.Mappings
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            // Camper mappings
            CreateMap<Camper, CamperResponseDto>();
            CreateMap<CamperCreateDto, Camper>();
            CreateMap<CamperUpdateDto, Camper>();

            //Guardian mappings
            CreateMap<Guardian, GuardianResponseDto>()
            .ForMember(dest => dest.Campers,
                opt => opt.MapFrom(src => src.CamperGuardians.Select(cg => cg.camper)));

            CreateMap<Camper, CamperSummaryDto>();
            CreateMap<GuardianCreateDto, Guardian>();
            CreateMap<GuardianUpdateDto, Guardian>();
        }
    }
}
