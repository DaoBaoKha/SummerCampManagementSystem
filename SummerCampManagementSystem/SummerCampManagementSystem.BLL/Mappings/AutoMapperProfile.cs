using AutoMapper;
using SummerCampManagementSystem.BLL.DTOs.Activity;
using SummerCampManagementSystem.BLL.DTOs.ActivitySchedule;
using SummerCampManagementSystem.BLL.DTOs.Album;
using SummerCampManagementSystem.BLL.DTOs.AttendanceLog;
using SummerCampManagementSystem.BLL.DTOs.Camp;
using SummerCampManagementSystem.BLL.DTOs.Camper;
using SummerCampManagementSystem.BLL.DTOs.CamperActivity;
using SummerCampManagementSystem.BLL.DTOs.CampType;
using SummerCampManagementSystem.BLL.DTOs.Guardian;
using SummerCampManagementSystem.BLL.DTOs.HealthRecord;
using SummerCampManagementSystem.BLL.DTOs.Location;
using SummerCampManagementSystem.BLL.DTOs.Promotion;
using SummerCampManagementSystem.BLL.DTOs.PromotionType;
using SummerCampManagementSystem.BLL.DTOs.Registration;
using SummerCampManagementSystem.BLL.DTOs.Route;
using SummerCampManagementSystem.BLL.DTOs.Transaction;
using SummerCampManagementSystem.BLL.DTOs.User;
using SummerCampManagementSystem.BLL.DTOs.UserAccount;
using SummerCampManagementSystem.BLL.DTOs.Vehicle;
using SummerCampManagementSystem.BLL.DTOs.VehicleType;
using SummerCampManagementSystem.DAL.Models;

namespace SummerCampManagementSystem.BLL.Mappings
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            // Camper mappings

            CreateMap<CamperRequestDto, Camper>();
            CreateMap<Camper, CamperResponseDto>();

            // HealthRecord mappings
            CreateMap<HealthRecordCreateDto, HealthRecord>();
            CreateMap<HealthRecord, HealthRecordResponseDto>();

            //Guardian mappings
            CreateMap<Guardian, GuardianResponseDto>()
            .ForMember(dest => dest.Campers,
                opt => opt.MapFrom(src => src.CamperGuardians.Select(cg => cg.camper)));

            CreateMap<Camper, CamperSummaryDto>();
            CreateMap<GuardianCreateDto, Guardian>();
            CreateMap<GuardianUpdateDto, Guardian>();

            // Camp mappings
            CreateMap<CampType, CampTypeDto>()
                .ForMember(dest => dest.Id,
                           opt => opt.MapFrom(src => src.campTypeId));

            CreateMap<CampRequestDto, Camp>();

            // Location mappings
            CreateMap<Location, LocationDto>()
                .ForMember(dest => dest.Id,
                           opt => opt.MapFrom(src => src.locationId));

            // Promotion mappings
            CreateMap<Promotion, PromotionDto>()
                .ForMember(dest => dest.Id,
                           opt => opt.MapFrom(src => src.promotionId));

            CreateMap<Camp, CampResponseDto>()
                .ForMember(dest => dest.CampType,
                           opt => opt.MapFrom(src => src.campType))

                .ForMember(dest => dest.Location,
                           opt => opt.MapFrom(src => src.location))

                .ForMember(dest => dest.Promotion,
                           opt => opt.MapFrom(src => src.promotion));

            CreateMap<Promotion, PromotionSummaryDto>()
                .ForMember(dest => dest.PromotionId, opt => opt.MapFrom(src => src.promotionId))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.name))
                .ForMember(dest => dest.Percent, opt => opt.MapFrom(src => src.percent));


            //Registration mapping
            CreateMap<Registration, RegistrationResponseDto>()
                .ForMember(dest => dest.CampName, opt => opt.MapFrom(src => src.camp.name))
                .ForMember(dest => dest.Campers, opt => opt.MapFrom(src => src.campers))
                .ForMember(dest => dest.AppliedPromotion, opt => opt.MapFrom(src => src.appliedPromotion))
                .ForMember(dest => dest.OptionalChoices, opt => opt.MapFrom(src => src.RegistrationOptionalActivities)); 

            CreateMap<RegistrationOptionalActivity, OptionalActivityChoiceSummaryDto>()
                .ForMember(dest => dest.ActivityName, opt => opt.MapFrom(src => src.activitySchedule.activity.name));

            //Route mappings
            CreateMap<RouteRequestDto, Route>();
            CreateMap<Route, RouteResponseDto>()
                .ForMember(dest => dest.CampName, opt => opt.MapFrom(src => src.camp != null ? src.camp.name : string.Empty));


            //Activity mappings
            CreateMap<Activity, ActivityResponseDto>();
            CreateMap<ActivityCreateDto, Activity>();
            CreateMap<Activity, ActivitySummaryDto>();


            //ActivitySchedule mappings
            CreateMap<ActivitySchedule, ActivityScheduleResponseDto>();

            CreateMap<ActivitySchedule, ActivityScheduleByCamperResponseDto>();

            CreateMap<ActivityScheduleCreateDto, ActivitySchedule>()
                .ForMember(dest => dest.isLivestream, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.status, opt => opt.MapFrom(src => "Draft"));

            CreateMap<OptionalScheduleCreateDto, ActivitySchedule>()
               .ForMember(dest => dest.isOptional, opt => opt.MapFrom(src => true))
               .ForMember(dest => dest.isLivestream, opt => opt.MapFrom(src => false))
               .ForMember(dest => dest.status, opt => opt.MapFrom(src => "Draft"));

        
            //CamperActivity mappings
            CreateMap<CamperActivity, CamperActivityResponseDto>()
                .ForMember(dest => dest.Camper, opt => opt.MapFrom(src => src.camper))
                .ForMember(dest => dest.Activity, opt => opt.MapFrom(src => src.activitySchedule));


            CreateMap<CamperActivityCreateDto, CamperActivity>()
                .ForMember(dest => dest.participationStatus, opt => opt.MapFrom(src => "Approved"));
            CreateMap<CamperActivityUpdateDto, CamperActivity>();

            //Promotion mappings
            CreateMap<Promotion, PromotionResponseDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.promotionId));

            // Map request DTO to entity with conversions for date and naming differences
            CreateMap<PromotionRequestDto, Promotion>()
                .ForMember(dest => dest.promotionTypeId, opt => opt.MapFrom(src => src.PromotionTypeId));


            CreateMap<PromotionType, PromotionTypeNameResponseDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.promotionTypeId));

            // RegistrationStaff mappings
            CreateMap<RegisterStaffRequestDto, UserAccount>()
                .ForMember(dest => dest.password, opt => opt.Ignore()) // sẽ hash thủ công
                .ForMember(dest => dest.createAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.isActive, opt => opt.MapFrom(_ => true));

            //Vehicle mappings
            CreateMap<VehicleRequestDto, Vehicle>();
            CreateMap<Vehicle, VehicleResponseDto>();

            //VehicleType mappings
            CreateMap<VehicleTypeRequestDto, VehicleType>()
                .ForMember(dest => dest.isActive, opt => opt.MapFrom(_ => true));


            CreateMap<VehicleType, VehicleTypeResponseDto>();

            // Location mappings
            CreateMap<LocationRequestDto, Location>();
            CreateMap<Location, LocationResponseDto>();

            CreateMap<AttendanceLog, AttendanceLogDto>()
          .ForMember(dest => dest.CamperName, opt => opt.MapFrom(src => src.staff.firstName + " " + src.staff.lastName));
         

            CreateMap<Location, LocationDto>()
                .ForMember(dest => dest.Id,
                            opt => opt.MapFrom(src => src.locationId));

            // Album mappings
            CreateMap<AlbumRequestDto, Album>()
                .ForMember(dest => dest.campId, opt => opt.MapFrom(src => src.CampId));

            CreateMap<Album, AlbumResponseDto>()
                .ForMember(dest => dest.CampName, opt => opt.MapFrom(src => src.camp.name))
                .ForMember(dest => dest.PhotoCount, opt => opt.MapFrom(src => src.AlbumPhotos.Count));

            // Transaction mappings 
            CreateMap<Transaction, TransactionResponseDto>()
                .ForMember(dest => dest.RegistrationId, opt => opt.MapFrom(src => src.registrationId))
                .ForMember(dest => dest.CampName, opt => opt.MapFrom(src => src.registration.camp.name)) 
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.registration.userId))    
                .ForMember(dest => dest.TransactionId, opt => opt.MapFrom(src => src.transactionId))
                .ForMember(dest => dest.TransactionTime, opt => opt.MapFrom(src => src.transactionTime));

            // UserAccount mappings
            CreateMap<UserAccount, UserResponseDto>();
            CreateMap<UserProfileUpdateDto, UserAccount>();
        }
    }
}
