using AutoMapper;
using SummerCampManagementSystem.BLL.DTOs.Accommodation;
using SummerCampManagementSystem.BLL.DTOs.AccommodationType;
using SummerCampManagementSystem.BLL.DTOs.Activity;
using SummerCampManagementSystem.BLL.DTOs.ActivitySchedule;
using SummerCampManagementSystem.BLL.DTOs.Album;
using SummerCampManagementSystem.BLL.DTOs.AlbumPhoto;
using SummerCampManagementSystem.BLL.DTOs.AttendanceLog;
using SummerCampManagementSystem.BLL.DTOs.Camp;
using SummerCampManagementSystem.BLL.DTOs.Camper;
using SummerCampManagementSystem.BLL.DTOs.CamperActivity;
using SummerCampManagementSystem.BLL.DTOs.CamperGroup;
using SummerCampManagementSystem.BLL.DTOs.CampStaffAssignment;
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
using SummerCampManagementSystem.Core.Enums;
using SummerCampManagementSystem.DAL.Models;
using static SummerCampManagementSystem.BLL.DTOs.Location.LocationRequestDto;

namespace SummerCampManagementSystem.BLL.Mappings
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            //Accommodation mappings
            CreateMap<Accommodation, AccommodationResponseDto>()
                .ForMember(dest => dest.supervisor,
                           opt => opt.MapFrom(src => src.supervisor != null
                               ? new SupervisorDto
                               {
                                   UserId = src.supervisor.userId,
                                   FullName = src.supervisor.lastName + " " + src.supervisor.firstName
                               }
                               : null));

            CreateMap<AccommodationRequestDto, Accommodation>()
                .ForMember(dest => dest.isActive, opt => opt.MapFrom(_ => true));


            // AccommodationType mappings
            CreateMap<AccommodationType, AccommodationTypeResponseDto>()
                .ForMember(dest => dest.Id,
                           opt => opt.MapFrom(src => src.accommodationTypeId));
            CreateMap<AccommodationTypeRequestDto, AccommodationType>()
                .ForMember(dest => dest.isActive, opt => opt.MapFrom(_ => true));

            // Camper mappings
            CreateMap<Camper, CamperSummaryDto>();
            CreateMap<CamperRequestDto, Camper>();
            CreateMap<Camper, CamperResponseDto>()
                .ForMember(dest => dest.Age, opt => opt.MapFrom(src =>
                    src.dob.HasValue
                        ? DateTime.Now.Year - src.dob.Value.Year -
                          (DateTime.Now.DayOfYear < src.dob.Value.DayOfYear ? 1 : 0)
                        : 0
            ));
            CreateMap<Camper, CamperWithGuardiansResponseDto>()
                .ForMember(dest => dest.Guardians,
                    opt => opt.MapFrom(src => src.CamperGuardians.Select(cg => cg.guardian)));

            // CamperGroup mapping
            CreateMap<CamperGroup, CamperGroupResponseDto>()
                 .ForMember(dest => dest.SupervisorName, opt => opt.MapFrom(src => src.supervisor.lastName + " " + src.supervisor.firstName));
            CreateMap<CamperGroupRequestDto, CamperGroup>();
            CreateMap<CamperGroup, CamperGroupWithCampDetailsResponseDto>()
                .ForMember(dest => dest.CampName,
                           opt => opt.MapFrom(src => src.camp != null ? src.camp.name : string.Empty));


            // HealthRecord mappings
            CreateMap<HealthRecordCreateDto, HealthRecord>();
            CreateMap<HealthRecord, HealthRecordResponseDto>();

            //Guardian mappings
            CreateMap<Guardian, GuardianResponseDto>()
            .ForMember(dest => dest.Campers,
                opt => opt.MapFrom(src => src.CamperGuardians.Select(cg => cg.camper)));
            CreateMap<Guardian, GuardianSummaryDto>();
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

            CreateMap<LocationCreateDto, Location>()
            .ForMember(dest => dest.campLocationId, opt => opt.MapFrom(src => src.ParentLocationId))
            .ForMember(dest => dest.locationType, opt => opt.MapFrom(src => src.LocationType.ToString()))
            .ForMember(dest => dest.isActive, opt => opt.MapFrom(src => true));
           
            CreateMap<LocationUpdateDto, Location>()
                .ForMember(dest => dest.campLocationId, opt => opt.MapFrom(src => src.ParentLocationId))
                .ForMember(dest => dest.locationType, opt => opt.MapFrom(src => src.LocationType.ToString()))
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

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

            CreateMap<Promotion, PromotionSummaryForCampDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.promotionId))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.name))
                .ForMember(dest => dest.Percent, opt => opt.MapFrom(src => src.percent));


            //Registration mapping
            CreateMap<Registration, RegistrationResponseDto>()
                .ForMember(dest => dest.CampName, opt => opt.MapFrom(src => src.camp.name))
                .ForMember(dest => dest.Campers, opt => opt.MapFrom(src => src.RegistrationCampers.Select(rc => rc.camper)))
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
            CreateMap<ActivitySchedule, ActivityScheduleResponseDto>()
                .ForMember(dest => dest.StaffName, opt => opt.MapFrom(src => src.staff.lastName + " " + src.staff.firstName))
                .ForMember(dest => dest.locationName, opt => opt.MapFrom(src => src.location.name));

            CreateMap<ActivitySchedule, ActivityScheduleByCamperResponseDto>()
                .IncludeBase<ActivitySchedule, ActivityScheduleResponseDto>();
                

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

            // CampStaffAssignment mappings
            CreateMap<UserAccount, StaffSummaryDto>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.userId))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.lastName + " " + src.firstName))
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.role));

            CreateMap<Camp, CampSummaryDto>();

            CreateMap<CampStaffAssignment, CampStaffAssignmentResponseDto>()
                .ForMember(dest => dest.CampStaffAssignmentId, opt => opt.MapFrom(src => src.campStaffAssignmentId));

            CreateMap<CampStaffAssignmentRequestDto, CampStaffAssignment>();

            CreateMap<CampStaffAssignment, CampStaffSummaryDto>();

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

            CreateMap<Location, LocationResponseDto>()
                .ForMember(dest => dest.LocationId, opt => opt.MapFrom(src => src.locationId))
                .ForMember(dest => dest.ParentLocationId, opt => opt.MapFrom(src => src.campLocationId))
                .ForMember(dest => dest.ParentLocationName,
                           opt => opt.MapFrom(src => src.campLocation != null ? src.campLocation.name : null));

            // AttendanceLog mappings
            CreateMap<AttendanceLog, AttendanceLogResponseDto>()
          .ForMember(dest => dest.CamperName, opt => opt.MapFrom(src => src.staff.lastName + " " + src.staff.firstName));
            CreateMap<AttendanceLogRequestDto, AttendanceLog>()
                .ForMember(dest => dest.checkInMethod, opt => opt.MapFrom(_ => "Manual"));
         

            CreateMap<Location, LocationDto>()
                .ForMember(dest => dest.Id,
                            opt => opt.MapFrom(src => src.locationId));

            // Album mappings
            CreateMap<AlbumRequestDto, Album>()
                .ForMember(dest => dest.campId, opt => opt.MapFrom(src => src.CampId));

            CreateMap<Album, AlbumResponseDto>()
                .ForMember(dest => dest.CampName, opt => opt.MapFrom(src => src.camp.name))
                .ForMember(dest => dest.PhotoCount, opt => opt.MapFrom(src => src.AlbumPhotos.Count));

            // AlbumPhoto mappings
            CreateMap<AlbumPhoto, AlbumPhotoResponseDto>();
            CreateMap<AlbumPhotoRequestDto, AlbumPhoto>();

            // Transaction mappings 
            CreateMap<Transaction, TransactionResponseDto>()
                .ForMember(dest => dest.RegistrationId, opt => opt.MapFrom(src => src.registrationId))
                .ForMember(dest => dest.CampName, opt => opt.MapFrom(src => src.registration.camp.name)) 
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.registration.userId))    
                .ForMember(dest => dest.TransactionId, opt => opt.MapFrom(src => src.transactionId))
                .ForMember(dest => dest.TransactionTime, opt => opt.MapFrom(src => src.transactionTime));

            // UserAccount mappings
            CreateMap<UserAccount, UserResponseDto>()
                .ForMember(
                    dest => dest.DateOfBirth,  // UserResponseDto
                    opt => opt.MapFrom(src => src.dob) // UserAccount
                );
            CreateMap<UserProfileUpdateDto, UserAccount>();
        }
    }
}
