namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface ICampReportExportService
    {
        Task<byte[]> ExportCampReportToExcelAsync(int campId);
        Task<byte[]> ExportCampReportToPdfAsync(int campId);
    }
}
