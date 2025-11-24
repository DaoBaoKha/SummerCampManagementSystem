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

        public ReportService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ReportResponseDto> CreateReportAsync(ReportRequestDto reportRequestDto, int staffId)
        {
            var report = _mapper.Map<Report>(reportRequestDto);
            report.reportedBy = staffId;
            await _unitOfWork.Reports.CreateAsync(report);
            await _unitOfWork.CommitAsync();
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

            var oldReportedBy = report.reportedBy;
            _mapper.Map(reportRequestDto, report);
            report.reportedBy = oldReportedBy;
            await _unitOfWork.Reports.UpdateAsync(report);
            await _unitOfWork.CommitAsync();
            return _mapper.Map<ReportResponseDto>(report);
        }
    }
}
