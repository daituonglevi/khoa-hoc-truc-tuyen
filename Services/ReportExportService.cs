using ClosedXML.Excel;
using System.IO;
using ELearningWebsite.Areas.Admin.ViewModels;

namespace ELearningWebsite.Services
{
    public interface IReportExportService
    {
        byte[] ExportToExcel(string reportType, object data);
    }

    public class ReportExportService : IReportExportService
    {
        public byte[] ExportToExcel(string reportType, object data)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add(GetReportTitle(reportType));

                // Add content based on report type
                switch (reportType.ToLower())
                {
                    case "user":
                        AddUserReportExcel(worksheet, (UserReportViewModel)data);
                        break;
                    case "course":
                        AddCourseReportExcel(worksheet, (CourseReportViewModel)data);
                        break;
                    case "finance":
                        AddFinanceReportExcel(worksheet, (FinanceReportViewModel)data);
                        break;
                }

                using (var ms = new MemoryStream())
                {
                    workbook.SaveAs(ms);
                    return ms.ToArray();
                }
            }
        }

        private string GetReportTitle(string reportType)
        {
            switch (reportType.ToLower())
            {
                case "user":
                    return "Báo cáo Người dùng";
                case "course":
                    return "Báo cáo Khóa học";
                case "finance":
                    return "Báo cáo Tài chính";
                default:
                    return "Báo cáo";
            }
        }

        private void AddUserReportExcel(IXLWorksheet worksheet, UserReportViewModel data)
        {
            // Add title
            worksheet.Cell("A1").Value = "Báo cáo Người dùng";
            worksheet.Range("A1:D1").Merge();
            
            // Add statistics
            worksheet.Cell("A3").Value = "Thđng kê";
            worksheet.Cell("A4").Value = "T�.ng sđ người dùng";
            worksheet.Cell("B4").Value = data.TotalUsers;
            worksheet.Cell("A5").Value = "Đã xác thực";
            worksheet.Cell("B5").Value = data.VerifiedUsers;
            worksheet.Cell("A6").Value = "Chưa xác thực";
            worksheet.Cell("B6").Value = data.UnverifiedUsers;

            // Add registration trend
            if (data.RegistrationTrend?.Any() == true)
            {
                worksheet.Cell("A8").Value = "Xu hư�>ng đ�fng ký";
                worksheet.Cell("A9").Value = "Tháng";
                worksheet.Cell("B9").Value = "Người dùng m�>i";

                var row = 10;
                foreach (var item in data.RegistrationTrend)
                {
                    worksheet.Cell($"A{row}").Value = item.Month;
                    worksheet.Cell($"B{row}").Value = item.NewUsers;
                    row++;
                }
            }

            // Format worksheet
            worksheet.Columns().AdjustToContents();
            worksheet.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Range("A1:D1").Style.Font.Bold = true;
            worksheet.Range("A3:B3").Style.Font.Bold = true;
            worksheet.Range("A8:B9").Style.Font.Bold = true;
        }

        private void AddCourseReportExcel(IXLWorksheet worksheet, CourseReportViewModel data)
        {
            // Add title
            worksheet.Cell("A1").Value = "Báo cáo Khóa học";
            worksheet.Range("A1:D1").Merge();
            
            // Add statistics
            worksheet.Cell("A3").Value = "Thđng kê";
            worksheet.Cell("A4").Value = "T�.ng sđ khóa học";
            worksheet.Cell("B4").Value = data.TotalCourses;
            worksheet.Cell("A5").Value = "Đã xuất bản";
            worksheet.Cell("B5").Value = data.PublishedCourses;
            worksheet.Cell("A6").Value = "Bản nháp";
            worksheet.Cell("B6").Value = data.DraftCourses;
            worksheet.Cell("A7").Value = "Giá trung bình";
            worksheet.Cell("B7").Value = data.AveragePrice;
            worksheet.Cell("B7").Style.NumberFormat.Format = "#,##0";

            // Add courses by category
            if (data.CoursesByCategory?.Any() == true)
            {
                worksheet.Cell("A9").Value = "Khóa học theo danh mục";
                worksheet.Cell("A10").Value = "Danh mục";
                worksheet.Cell("B10").Value = "T�.ng khóa học";
                worksheet.Cell("C10").Value = "Đã xuất bản";
                worksheet.Cell("D10").Value = "T�.ng ghi danh";

                var row = 11;
                foreach (var category in data.CoursesByCategory)
                {
                    worksheet.Cell($"A{row}").Value = category.CategoryName;
                    worksheet.Cell($"B{row}").Value = category.CourseCount;
                    worksheet.Cell($"C{row}").Value = category.PublishedCourses;
                    worksheet.Cell($"D{row}").Value = category.TotalEnrollments;
                    row++;
                }
            }

            // Format worksheet
            worksheet.Columns().AdjustToContents();
            worksheet.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Range("A1:D1").Style.Font.Bold = true;
            worksheet.Range("A3:B3").Style.Font.Bold = true;
            worksheet.Range("A9:D10").Style.Font.Bold = true;
        }

        private void AddFinanceReportExcel(IXLWorksheet worksheet, FinanceReportViewModel data)
        {
            // Add title
            worksheet.Cell("A1").Value = "Báo cáo Tài chính";
            worksheet.Range("A1:E1").Merge();
            
            // Add statistics
            worksheet.Cell("A3").Value = "Thđng kê";
            worksheet.Cell("A4").Value = "T�.ng doanh thu";
            worksheet.Cell("B4").Value = data.TotalRevenue;
            worksheet.Cell("A5").Value = "T�.ng phí";
            worksheet.Cell("B5").Value = data.TotalFees;
            worksheet.Cell("A6").Value = "Doanh thu ròng";
            worksheet.Cell("B6").Value = data.NetRevenue;
            worksheet.Cell("A7").Value = "T�.ng giao d�<ch";
            worksheet.Cell("B7").Value = data.TotalTransactions;

            // Format currency cells
            worksheet.Range("B4:B6").Style.NumberFormat.Format = "#,##0";

            // Add revenue trend
            if (data.RevenueTrend?.Any() == true)
            {
                worksheet.Cell("A9").Value = "Xu hư�>ng doanh thu";
                worksheet.Cell("A10").Value = "Tháng";
                worksheet.Cell("B10").Value = "Doanh thu";
                worksheet.Cell("C10").Value = "Phí";
                worksheet.Cell("D10").Value = "Doanh thu ròng";
                worksheet.Cell("E10").Value = "Tỷ suất lợi nhuận";

                var row = 11;
                foreach (var item in data.RevenueTrend)
                {
                    worksheet.Cell($"A{row}").Value = item.Month;
                    worksheet.Cell($"B{row}").Value = item.Revenue;
                    worksheet.Cell($"C{row}").Value = item.Fees;
                    worksheet.Cell($"D{row}").Value = item.NetRevenue;
                    worksheet.Cell($"E{row}").FormulaA1 = $"=IF(B{row}=0,0,D{row}/B{row}*100)";
                    
                    // Format cells
                    worksheet.Range($"B{row}:D{row}").Style.NumberFormat.Format = "#,##0";
                    worksheet.Cell($"E{row}").Style.NumberFormat.Format = "0.00%";
                    row++;
                }
            }

            // Format worksheet
            worksheet.Columns().AdjustToContents();
            worksheet.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Range("A1:E1").Style.Font.Bold = true;
            worksheet.Range("A3:B3").Style.Font.Bold = true;
            worksheet.Range("A9:E10").Style.Font.Bold = true;
        }
    }
} 