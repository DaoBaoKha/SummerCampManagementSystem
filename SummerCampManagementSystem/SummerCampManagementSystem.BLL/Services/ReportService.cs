using AutoMapper;
using SummerCampManagementSystem.BLL.DTOs.Activity;
using SummerCampManagementSystem.BLL.DTOs.Report;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
