using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SummerCampManagementSystem.BLL.DTOs.StatisticalReport;
using SummerCampManagementSystem.BLL.Exceptions;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.DAL.UnitOfWork;

namespace SummerCampManagementSystem.BLL.Services
{
    public class CampReportExportService : ICampReportExportService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CampReportExportService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            // configure QuestPDF license
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public async Task<byte[]> ExportCampReportToExcelAsync(int campId)
        {
            // validate camp exists
            var camp = await _unitOfWork.Camps.GetByIdAsync(campId);
            if (camp == null)
                throw new NotFoundException($"Camp with ID {campId} not found");

            // get all data from repository
            var overviewData = await _unitOfWork.Camps.GetCampReportOverviewAsync(campId);
            var funnelData = await _unitOfWork.Camps.GetRegistrationFunnelDataAsync(campId);
            var rosterData = await _unitOfWork.Camps.GetCamperRosterDataAsync(campId);
            var transactionData = await _unitOfWork.Camps.GetFinancialTransactionsAsync(campId);
            var staffData = await _unitOfWork.Camps.GetStaffAssignmentsAsync(campId);

            // calculate additional metrics
            var confirmedCampers = rosterData.Count;
            var occupancyRate = overviewData.MaxParticipants > 0 
                ? (decimal)confirmedCampers / overviewData.MaxParticipants * 100 
                : 0;

            // calculate revenue from CONFIRMED transactions only
            // Payment type = add to revenue, Refund type = subtract from revenue
            var confirmedTransactions = transactionData.Where(t => t.Status == "Confirmed").ToList();
            var totalRevenue = confirmedTransactions.Where(t => t.Type == "Payment").Sum(t => Math.Abs(t.Amount));
            var totalRefunds = confirmedTransactions.Where(t => t.Type == "Refund").Sum(t => Math.Abs(t.Amount));
            var netRevenue = totalRevenue - totalRefunds;

            // create workbook
            using var workbook = new XLWorkbook();

            // SHEET 1: OVERVIEW & KPIs
            CreateOverviewSheet(workbook, overviewData, confirmedCampers, occupancyRate, totalRevenue, totalRefunds, netRevenue);

            // SHEET 2: REGISTRATION FUNNEL
            CreateRegistrationFunnelSheet(workbook, funnelData);

            // SHEET 3: CAMPER ROSTER
            CreateCamperRosterSheet(workbook, rosterData);

            // SHEET 4: FINANCIAL TRANSACTIONS
            CreateFinancialTransactionsSheet(workbook, transactionData);

            // SHEET 5: STAFF ASSIGNMENTS
            CreateStaffAssignmentSheet(workbook, staffData);

            // save to memory stream
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private void CreateOverviewSheet(XLWorkbook workbook, 
            (string CampName, string CampType, string Location, DateTime? StartDate, DateTime? EndDate, string Status, int MaxParticipants, decimal? AverageRating, int TotalFeedbacks) overview,
            int confirmedCampers, decimal occupancyRate, decimal totalRevenue, decimal totalRefunds, decimal netRevenue)
        {
            var ws = workbook.Worksheets.Add("Tổng Quan");

            // title
            ws.Cell(1, 1).Value = "BÁO CÁO TỔNG QUAN TRẠI HÈ";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 16;
            ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Range(1, 1, 1, 3).Merge();

            // system branding
            ws.Cell(2, 1).Value = "Hệ thống CampEase Summer Camp Management";
            ws.Cell(2, 1).Style.Font.Italic = true;
            ws.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Range(2, 1, 2, 3).Merge();

            int row = 4;

            // camp basic info section
            ws.Cell(row, 1).Value = "THÔNG TIN CƠ BẢN";
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
            ws.Range(row, 1, row, 3).Merge();
            row++;

            AddMetricRow(ws, row++, "Tên Trại", overview.CampName);
            AddMetricRow(ws, row++, "Loại Trại", overview.CampType);
            AddMetricRow(ws, row++, "Địa Điểm", overview.Location);
            AddMetricRow(ws, row++, "Ngày Bắt Đầu", overview.StartDate?.ToString("dd/MM/yyyy") ?? "N/A");
            AddMetricRow(ws, row++, "Ngày Kết Thúc", overview.EndDate?.ToString("dd/MM/yyyy") ?? "N/A");
            AddMetricRow(ws, row++, "Trạng Thái", overview.Status);

            row++;

            // capacity metrics section
            ws.Cell(row, 1).Value = "CHỈ SỐ NĂNG LỰC";
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightGreen;
            ws.Range(row, 1, row, 3).Merge();
            row++;

            AddMetricRow(ws, row++, "Số Trại Viên Đã Xác Nhận", confirmedCampers.ToString());
            AddMetricRow(ws, row++, "Sức Chứa Tối Đa", overview.MaxParticipants.ToString());
            AddMetricRow(ws, row++, "Tỷ Lệ Lấp Đầy", $"{occupancyRate:F2}%");

            row++;

            // financial metrics section
            ws.Cell(row, 1).Value = "CHỈ SỐ TÀI CHÍNH";
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightYellow;
            ws.Range(row, 1, row, 3).Merge();
            row++;

            AddMetricRow(ws, row++, "Tổng Doanh Thu", $"{totalRevenue:N0} VND");
            AddMetricRow(ws, row++, "Tổng Hoàn Tiền", $"{totalRefunds:N0} VND");
            AddMetricRow(ws, row++, "Doanh Thu Ròng", $"{netRevenue:N0} VND");

            row++;

            // performance metrics section
            ws.Cell(row, 1).Value = "CHỈ SỐ HIỆU SUẤT";
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightCoral;
            ws.Range(row, 1, row, 3).Merge();
            row++;

            AddMetricRow(ws, row++, "Đánh Giá Trung Bình", overview.AverageRating.HasValue ? $"{overview.AverageRating:F2} / 5.0" : "Chưa có");
            AddMetricRow(ws, row++, "Tổng Số Đánh Giá", overview.TotalFeedbacks.ToString());

            // auto-fit columns
            ws.Columns().AdjustToContents();
        }

        private void CreateRegistrationFunnelSheet(XLWorkbook workbook,
            (int TotalRequests, int PendingApproval, int Approved, int Confirmed, int Canceled, int Rejected) funnel)
        {
            var ws = workbook.Worksheets.Add("Phân Tích Đăng Ký");

            // title
            ws.Cell(1, 1).Value = "PHÂN TÍCH PHIẾU ĐĂNG KÝ";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;
            ws.Range(1, 1, 1, 4).Merge();

            // header row
            int row = 3;
            ws.Cell(row, 1).Value = "Trạng Thái";
            ws.Cell(row, 2).Value = "Số Lượng";
            ws.Cell(row, 3).Value = "Tỷ Lệ (%)";
            ws.Cell(row, 4).Value = "Mô Tả";

            var headerRange = ws.Range(row, 1, row, 4);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.DarkBlue;
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thick;

            row++;

            // data rows
            AddFunnelRow(ws, row++, "Tổng Số Yêu Cầu", funnel.TotalRequests, 100, "Tổng số người quan tâm ban đầu");
            AddFunnelRow(ws, row++, "Chờ Duyệt", funnel.PendingApproval, CalculatePercentage(funnel.PendingApproval, funnel.TotalRequests), "Đang xử lý");
            AddFunnelRow(ws, row++, "Được Duyệt", funnel.Approved, CalculatePercentage(funnel.Approved, funnel.TotalRequests), "Khách hàng đã được duyệt sẵn sàng thanh toán");
            AddFunnelRow(ws, row++, "Đã Xác Nhận", funnel.Confirmed, CalculatePercentage(funnel.Confirmed, funnel.TotalRequests), "Khách hàng đã thanh toán");
            AddFunnelRow(ws, row++, "Đã Hủy", funnel.Canceled, CalculatePercentage(funnel.Canceled, funnel.TotalRequests), "Hủy đăng ký");
            AddFunnelRow(ws, row++, "Bị Từ Chối", funnel.Rejected, CalculatePercentage(funnel.Rejected, funnel.TotalRequests), "Do không đủ điều kiện");

            // auto-fit columns
            ws.Columns().AdjustToContents();
        }

        private void CreateCamperRosterSheet(XLWorkbook workbook, List<(string CamperName, int Age, string Gender, string GuardianName, string GuardianPhone, string GroupName, string MedicalNotes, string TransportInfo, string AccommodationInfo)> roster)
        {
            var ws = workbook.Worksheets.Add("Danh Sách Trại Viên");

            // title
            ws.Cell(1, 1).Value = "DANH SÁCH TRẠI VIÊN CHI TIẾT";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;
            ws.Range(1, 1, 1, 10).Merge();

            // header row
            int row = 3;
            string[] headers = { "STT", "Tên Trại Viên", "Tuổi", "Giới Tính", "Phụ Huynh", "SĐT", "Nhóm", "Ghi Chú Y Tế", "Xe Đưa Đón", "Phòng" };
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(row, i + 1).Value = headers[i];
            }

            var headerRange = ws.Range(row, 1, row, headers.Length);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.DarkGreen;
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thick;

            row++;

            // data rows
            int stt = 1;
            foreach (var camper in roster)
            {
                ws.Cell(row, 1).Value = stt++;
                ws.Cell(row, 2).Value = camper.CamperName;
                ws.Cell(row, 3).Value = camper.Age;
                ws.Cell(row, 4).Value = camper.Gender;
                ws.Cell(row, 5).Value = camper.GuardianName;
                ws.Cell(row, 6).Value = camper.GuardianPhone;
                ws.Cell(row, 7).Value = camper.GroupName;
                ws.Cell(row, 8).Value = camper.MedicalNotes;
                ws.Cell(row, 9).Value = camper.TransportInfo;
                ws.Cell(row, 10).Value = camper.AccommodationInfo;

                // alternate row colors
                if (row % 2 == 0)
                {
                    ws.Range(row, 1, row, headers.Length).Style.Fill.BackgroundColor = XLColor.LightGray;
                }

                row++;
            }

            // auto-fit columns
            ws.Columns().AdjustToContents();
        }

        private void CreateFinancialTransactionsSheet(XLWorkbook workbook, List<(string TransactionCode, DateTime? TransactionDate, string PayerName, string Description, decimal Amount, string Status, string PaymentMethod, string Type)> transactions)
        {
            var ws = workbook.Worksheets.Add("Tài Chính");

            // title
            ws.Cell(1, 1).Value = "BÁO CÁO TÀI CHÍNH & GIAO DỊCH";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;
            ws.Range(1, 1, 1, 7).Merge();

            // header row
            int row = 3;
            string[] headers = { "Mã GD", "Ngày", "Người Trả", "Nội Dung", "Số Tiền (VND)", "Trạng Thái", "Cổng TT" };
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(row, i + 1).Value = headers[i];
            }

            var headerRange = ws.Range(row, 1, row, headers.Length);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.DarkOrange;
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thick;

            row++;

            // data rows
            foreach (var transaction in transactions)
            {
                ws.Cell(row, 1).Value = transaction.TransactionCode;
                ws.Cell(row, 2).Value = transaction.TransactionDate?.ToString("dd/MM/yyyy HH:mm") ?? "N/A";
                ws.Cell(row, 3).Value = transaction.PayerName;
                ws.Cell(row, 4).Value = transaction.Description;
                ws.Cell(row, 5).Value = transaction.Amount;
                ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0";
                ws.Cell(row, 6).Value = transaction.Status;
                ws.Cell(row, 7).Value = transaction.PaymentMethod;

                // color code amounts
                if (transaction.Amount < 0)
                {
                    ws.Cell(row, 5).Style.Font.FontColor = XLColor.Red;
                }
                else
                {
                    ws.Cell(row, 5).Style.Font.FontColor = XLColor.Green;
                }

                row++;
            }

            // auto-fit columns
            ws.Columns().AdjustToContents();
        }

        private void CreateStaffAssignmentSheet(XLWorkbook workbook, List<(string StaffName, string Role, string Email, string PhoneNumber, string AssignmentType)> staff)
        {
            var ws = workbook.Worksheets.Add("Nhân Viên");

            // title
            ws.Cell(1, 1).Value = "DANH SÁCH NHÂN VIÊN ĐƯỢC PHÂN CÔNG";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;
            ws.Range(1, 1, 1, 5).Merge();

            // header row
            int row = 3;
            string[] headers = { "STT", "Tên Nhân Viên", "Vai Trò", "Email", "SĐT", "Phân Công" };
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(row, i + 1).Value = headers[i];
            }

            var headerRange = ws.Range(row, 1, row, headers.Length);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.PurpleHeart;
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thick;

            row++;

            // data rows
            int stt = 1;
            foreach (var staffMember in staff)
            {
                ws.Cell(row, 1).Value = stt++;
                ws.Cell(row, 2).Value = staffMember.StaffName;
                ws.Cell(row, 3).Value = staffMember.Role;
                ws.Cell(row, 4).Value = staffMember.Email;
                ws.Cell(row, 5).Value = staffMember.PhoneNumber;
                ws.Cell(row, 6).Value = staffMember.AssignmentType;

                // alternate row colors
                if (row % 2 == 0)
                {
                    ws.Range(row, 1, row, headers.Length).Style.Fill.BackgroundColor = XLColor.LightGray;
                }

                row++;
            }

            // auto-fit columns
            ws.Columns().AdjustToContents();
        }

        private void AddMetricRow(IXLWorksheet ws, int row, string label, string value)
        {
            ws.Cell(row, 1).Value = label;
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 2).Value = value;
            ws.Range(row, 2, row, 3).Merge();
        }

        private void AddFunnelRow(IXLWorksheet ws, int row, string status, int count, decimal percentage, string meaning)
        {
            ws.Cell(row, 1).Value = status;
            ws.Cell(row, 2).Value = count;
            ws.Cell(row, 3).Value = $"{percentage:F1}%";
            ws.Cell(row, 4).Value = meaning;
        }

        private decimal CalculatePercentage(int part, int total)
        {
            return total > 0 ? (decimal)part / total * 100 : 0;
        }

        public async Task<byte[]> ExportCampReportToPdfAsync(int campId)
        {
            // validate camp exists
            var camp = await _unitOfWork.Camps.GetByIdAsync(campId);
            if (camp == null)
                throw new NotFoundException($"Camp with ID {campId} not found");

            // get all data from repository
            var overviewData = await _unitOfWork.Camps.GetCampReportOverviewAsync(campId);
            var funnelData = await _unitOfWork.Camps.GetRegistrationFunnelDataAsync(campId);
            var rosterData = await _unitOfWork.Camps.GetCamperRosterDataAsync(campId);
            var transactionData = await _unitOfWork.Camps.GetFinancialTransactionsAsync(campId);

            // calculate additional metrics
            var confirmedCampers = rosterData.Count;
            var occupancyRate = overviewData.MaxParticipants > 0
                ? (decimal)confirmedCampers / overviewData.MaxParticipants * 100
                : 0;

            var totalRevenue = transactionData.Where(t => t.Amount > 0).Sum(t => t.Amount);
            var totalRefunds = Math.Abs(transactionData.Where(t => t.Amount < 0).Sum(t => t.Amount));
            var netRevenue = totalRevenue - totalRefunds;

            // generate PDF
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    // header
                    page.Header()
                        .Text("BÁO CÁO TRẠI HÈ - CampEase System")
                        .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                    // content
                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(col =>
                        {
                            // overview section
                            col.Item().Text("TỔNG QUAN").Bold().FontSize(16);
                            col.Item().PaddingBottom(10);

                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Text($"Tên Trại: {overviewData.CampName}");
                            });
                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Text($"Loại Trại: {overviewData.CampType}");
                            });
                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Text($"Địa Điểm: {overviewData.Location}");
                            });
                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Text($"Thời Gian: {overviewData.StartDate?.ToString("dd/MM/yyyy")} - {overviewData.EndDate?.ToString("dd/MM/yyyy")}");
                            });

                            col.Item().PaddingTop(20);

                            // metrics section
                            col.Item().Text("CHỈ SỐ CHÍNH").Bold().FontSize(14);
                            col.Item().PaddingBottom(10);

                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                });

                                table.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Chỉ Số").Bold();
                                table.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Giá Trị").Bold();

                                table.Cell().BorderBottom(1).Padding(5).Text("Trại Viên Xác Nhận");
                                table.Cell().BorderBottom(1).Padding(5).Text(confirmedCampers.ToString());

                                table.Cell().BorderBottom(1).Padding(5).Text("Tỷ Lệ Lấp Đầy");
                                table.Cell().BorderBottom(1).Padding(5).Text($"{occupancyRate:F2}%");

                                table.Cell().BorderBottom(1).Padding(5).Text("Doanh Thu Ròng");
                                table.Cell().BorderBottom(1).Padding(5).Text($"{netRevenue:N0} VND");

                                table.Cell().Padding(5).Text("Đánh Giá TB");
                                table.Cell().Padding(5).Text(overviewData.AverageRating.HasValue ? $"{overviewData.AverageRating:F2}/5.0" : "Chưa có");
                            });
                        });

                    // footer
                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Trang ");
                            x.CurrentPageNumber();
                            x.Span(" / ");
                            x.TotalPages();
                        });
                });
            });

            return document.GeneratePdf();
        }
    }
}
