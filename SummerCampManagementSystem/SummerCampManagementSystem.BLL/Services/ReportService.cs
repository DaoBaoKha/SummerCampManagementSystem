using AutoMapper;
using SummerCampManagementSystem.BLL.DTOs.Report;
using SummerCampManagementSystem.BLL.Exceptions;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.Core.Enums;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;

namespace SummerCampManagementSystem.BLL.Services
{
    public class ReportService : IReportService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IUploadSupabaseService _uploadSupabaseService;

        public ReportService(IUnitOfWork unitOfWork, IMapper mapper, IUploadSupabaseService uploadSupabaseService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _uploadSupabaseService = uploadSupabaseService;
        }

        public async Task<ReportResponseDto> CreateReportAsync(ReportRequestDto reportRequestDto, int staffId)
        {
            var camper = await _unitOfWork.Campers.GetByIdAsync(reportRequestDto.camperId)
                ?? throw new KeyNotFoundException("Camper not found");

            var activity = await _unitOfWork.Activities.GetByIdAsync(reportRequestDto.activityId)
                ?? throw new KeyNotFoundException("Activity not found");

            if(!await _unitOfWork.Reports.IsCamperOfActivityAsync(camper.camperId, activity.activityId))
            {
                throw new InvalidOperationException("Camper did not participate in the activity");
            }

            using var transaction = await _unitOfWork.BeginTransactionAsync();

            var report = _mapper.Map<Report>(reportRequestDto);
            report.reportedBy = staffId;
            await _unitOfWork.Reports.CreateAsync(report);

            if (reportRequestDto.image != null)
            {
                var url = await _uploadSupabaseService.UploadReportCamperAsync(report.reportId, reportRequestDto.image);
                report.image = url;
            }

            await _unitOfWork.CommitAsync();
            await transaction.CommitAsync();

            return _mapper.Map<ReportResponseDto>(report);
        }

        public async Task<bool> DeleteReportAsync(int reportId)
        {
            var report = await _unitOfWork.Reports.GetByIdAsync(reportId);
            if (report == null) return false;

            await _unitOfWork.Reports.RemoveAsync(report);
            await _unitOfWork.CommitAsync();
            return true;
        }
        

        public async Task<IEnumerable<ReportResponseDto>> GetAllReportsAsync()
        {
            var reports = await _unitOfWork.Reports.GetAllAsync();
            return _mapper.Map<IEnumerable<ReportResponseDto>>(reports);
        }

        public async Task<ReportResponseDto?> GetReportByIdAsync(int reportId)
        {
            var report = await _unitOfWork.Reports.GetByIdAsync(reportId);
            return report == null ? null : _mapper.Map<ReportResponseDto>(report);

        }

        public async Task<ReportResponseDto?> UpdateReportAsync(int reportId, ReportRequestDto reportRequestDto)
        {
            var report = await _unitOfWork.Reports.GetByIdAsync(reportId);
            if (report == null) return null;

            var camper = await _unitOfWork.Campers.GetByIdAsync(reportRequestDto.camperId)
              ?? throw new KeyNotFoundException("Camper not found");

            var activity = await _unitOfWork.Activities.GetByIdAsync(reportRequestDto.activityId)
                ?? throw new KeyNotFoundException("Activity not found");

            if (!await _unitOfWork.Reports.IsCamperOfActivityAsync(camper.camperId, activity.activityId))
            {
                throw new InvalidOperationException("Camper did not participate in the activity");
            }

            var oldReportedBy = report.reportedBy;

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            _mapper.Map(reportRequestDto, report);
            report.reportedBy = oldReportedBy;
            await _unitOfWork.Reports.UpdateAsync(report);

            if (reportRequestDto.image != null)
            {
                var url = await _uploadSupabaseService.UploadReportCamperAsync(report.reportId, reportRequestDto.image);
                report.image = url;
            }

            await _unitOfWork.CommitAsync();
            await transaction.CommitAsync();

            return _mapper.Map<ReportResponseDto>(report);
        }

        public async Task<ReportResponseDto> CreateTransportIncidentAsync(TransportIncidentRequestDto dto, int staffId)
        {
            // validate camper exists
            var camper = await _unitOfWork.Campers.GetByIdAsync(dto.camperId)
                ?? throw new NotFoundException($"Camper with ID {dto.camperId} not found");

            // validate transport schedule exists
            var transportSchedule = await _unitOfWork.TransportSchedules.GetByIdAsync(dto.transportScheduleId)
                ?? throw new NotFoundException($"Transport schedule with ID {dto.transportScheduleId} not found");

            // find the CamperTransport record
            var camperTransport = await _unitOfWork.CamperTransports.GetCamperTransportByScheduleAndCamperAsync(
                dto.transportScheduleId, dto.camperId);
            
            if (camperTransport == null)
            {
                throw new NotFoundException($"Camper {dto.camperId} is not assigned to transport schedule {dto.transportScheduleId}");
            }

            // find the RegistrationCamper record
            var registrationCamper = await _unitOfWork.RegistrationCampers.GetByCamperIdAsync(dto.camperId);
            if (registrationCamper == null)
            {
                throw new NotFoundException($"Registration camper record not found for camper {dto.camperId}");
            }

            using var transaction = await _unitOfWork.BeginTransactionAsync();

            // create the report
            var report = new Report
            {
                camperId = dto.camperId,
                transportScheduleId = dto.transportScheduleId,
                reportType = "Transport",
                level = "3",
                note = dto.note,
                reportedBy = staffId,
                createAt = DateTime.UtcNow,
                status = "Active"
            };

            await _unitOfWork.Reports.CreateAsync(report);

            // set image URL if provided (already uploaded by frontend)
            if (!string.IsNullOrEmpty(dto.imageUrl))
            {
                report.image = dto.imageUrl;
            }

            /*
             * UPDATE STATUS
             * CamperTransport.status -> Canceled
             * RegistrationCamper.status -> Canceled
             */
            camperTransport.status = CamperTransportStatus.Canceled.ToString();
            await _unitOfWork.CamperTransports.UpdateAsync(camperTransport);

            registrationCamper.status = RegistrationCamperStatus.Canceled.ToString();
            await _unitOfWork.RegistrationCampers.UpdateAsync(registrationCamper);

            await _unitOfWork.CommitAsync();
            await transaction.CommitAsync();

            return _mapper.Map<ReportResponseDto>(report);
        }

        public async Task<ReportResponseDto> CreateEarlyCheckoutReportAsync(EarlyCheckoutRequestDto dto, int staffId)
        {
            // validate camper exists
            var camper = await _unitOfWork.Campers.GetByIdAsync(dto.camperId)
                ?? throw new NotFoundException($"Camper with ID {dto.camperId} not found");

            // find the RegistrationCamper record
            var registrationCamper = await _unitOfWork.RegistrationCampers.GetByCamperIdAsync(dto.camperId);
            if (registrationCamper == null)
            {
                throw new NotFoundException($"Registration camper record not found for camper {dto.camperId}");
            }

            // validate current status is CheckedIn
            if (registrationCamper.status != RegistrationCamperStatus.CheckedIn.ToString())
            {
                throw new BadRequestException($"Camper must be in CheckedIn status to perform early checkout. Current status: {registrationCamper.status}");
            }

            using var transaction = await _unitOfWork.BeginTransactionAsync();

            // create the report
            var report = new Report
            {
                camperId = dto.camperId,
                reportType = "CheckOut",
                level = "3",
                note = dto.note,
                reportedBy = staffId,
                createAt = DateTime.UtcNow,
                status = "Active"
            };

            await _unitOfWork.Reports.CreateAsync(report);

            // set image URL if provided (already uploaded by frontend)
            if (!string.IsNullOrEmpty(dto.imageUrl))
            {
                report.image = dto.imageUrl;
            }

            /*
             * UPDATE STATUS
             * RegistrationCamper.status: CheckedIn -> CheckedOut
             */
            registrationCamper.status = RegistrationCamperStatus.CheckedOut.ToString();
            await _unitOfWork.RegistrationCampers.UpdateAsync(registrationCamper);

            await _unitOfWork.CommitAsync();
            await transaction.CommitAsync();

            return _mapper.Map<ReportResponseDto>(report);
        }

        public async Task<ReportResponseDto> CreateIncidentTicketAsync(IncidentTicketRequestDto dto, int staffId)
        {
            // validate level is between 1 and 3
            if (dto.level < 1 || dto.level > 3)
            {
                throw new BadRequestException("Incident level must be between 1 and 3");
            }

            // validate camper exists
            var camper = await _unitOfWork.Campers.GetByIdAsync(dto.camperId)
                ?? throw new NotFoundException($"Camper with ID {dto.camperId} not found");

            // validate activity schedule if provided
            if (dto.activityScheduleId.HasValue)
            {
                var activitySchedule = await _unitOfWork.ActivitySchedules.GetByIdAsync(dto.activityScheduleId.Value);
                if (activitySchedule == null)
                {
                    throw new NotFoundException($"Activity schedule with ID {dto.activityScheduleId.Value} not found");
                }
            }

            using var transaction = await _unitOfWork.BeginTransactionAsync();

            // create the report
            var report = new Report
            {
                camperId = dto.camperId,
                activityScheduleId = dto.activityScheduleId,
                reportType = "Incident",
                level = dto.level.ToString(),
                note = dto.note,
                reportedBy = staffId,
                createAt = DateTime.UtcNow,
                status = "Active"
            };

            await _unitOfWork.Reports.CreateAsync(report);

            // set image URL if provided (already uploaded by frontend)
            if (!string.IsNullOrEmpty(dto.imageUrl))
            {
                report.image = dto.imageUrl;
            }

            await _unitOfWork.CommitAsync();
            await transaction.CommitAsync();

            return _mapper.Map<ReportResponseDto>(report);
        }

        public async Task<IEnumerable<ReportResponseDto>> GetReportsByCamperAsync(int camperId, int? campId = null)
        {
            // validate camper exists
            var camper = await _unitOfWork.Campers.GetByIdAsync(camperId)
                ?? throw new NotFoundException($"Camper with ID {camperId} not found");

            // validate camp if provided
            if (campId.HasValue)
            {
                var camp = await _unitOfWork.Camps.GetByIdAsync(campId.Value)
                    ?? throw new NotFoundException($"Camp with ID {campId.Value} not found");
            }

            var reports = await _unitOfWork.Reports.GetReportsByCamperAsync(camperId, campId);
            return _mapper.Map<IEnumerable<ReportResponseDto>>(reports);
        }

        public async Task<IEnumerable<ReportResponseDto>> GetReportsByStaffAsync(int staffId)
        {
            // validate staff exists
            var staff = await _unitOfWork.UserAccounts.GetByIdAsync(staffId)
                ?? throw new NotFoundException($"Staff with ID {staffId} not found");

            var reports = await _unitOfWork.Reports.GetReportsByStaffAsync(staffId);
            return _mapper.Map<IEnumerable<ReportResponseDto>>(reports);
        }

        public async Task<IEnumerable<ReportResponseDto>> GetReportsByCampAsync(int campId)
        {
            // validate camp exists
            var camp = await _unitOfWork.Camps.GetByIdAsync(campId)
                ?? throw new NotFoundException($"Camp with ID {campId} not found");

            var reports = await _unitOfWork.Reports.GetReportsByCampAsync(campId);
            return _mapper.Map<IEnumerable<ReportResponseDto>>(reports);
        }
    }
}
