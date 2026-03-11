using Microsoft.AspNetCore.Mvc;
using ELearningWebsite.Data;
using ELearningWebsite.Models;

namespace ELearningWebsite.Controllers
{
    public class DataFixController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DataFixController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> FixEncoding()
        {
            try
            {
                // Xóa dữ li�?u cũ
                _context.Chapters.RemoveRange(_context.Chapters);
                _context.Enrollments.RemoveRange(_context.Enrollments);
                _context.Finances.RemoveRange(_context.Finances);
                _context.Courses.RemoveRange(_context.Courses);
                _context.Categories.RemoveRange(_context.Categories);
                
                await _context.SaveChangesAsync();

                // Thêm Categories m�>i
                var categories = new List<Category>
                {
                    new Category { Name = "Lập trình Web", Description = "Các khóa học về phát tri�fn web frontend và backend", Status = 1 },
                    new Category { Name = "Lập trình Mobile", Description = "Các khóa học về phát tri�fn ứng dụng di đ�Tng", Status = 1 },
                    new Category { Name = "Cơ s�Y dữ li�?u", Description = "Các khóa học về quản tr�< và phát tri�fn cơ s�Y dữ li�?u", Status = 1 },
                    new Category { Name = "DevOps & Cloud", Description = "Các khóa học về DevOps và đi�?n toán đám mây", Status = 1 },
                    new Category { Name = "AI & Machine Learning", Description = "Các khóa học về trí tu�? nhân tạo và học máy", Status = 1 },
                    new Category { Name = "Blockchain & Web3", Description = "Các khóa học về blockchain và công ngh�? Web3", Status = 1 }
                };

                _context.Categories.AddRange(categories);
                await _context.SaveChangesAsync();

                // Thêm Courses m�>i
                var courses = new List<Course>
                {
                    new Course 
                    { 
                        Title = "Khóa học ASP.NET Core từ cơ bản đến nâng cao", 
                        CategoryId = 1, 
                        Price = 299000, 
                        Thumbnail = "https://via.placeholder.com/400x300/6366f1/ffffff?text=ASP.NET+Core", 
                        Description = "Học ASP.NET Core từ cơ bản đến nâng cao v�>i các dự án thực tế", 
                        Status = "Published", 
                        PreviewVideo = "https://www.youtube.com/watch?v=dQw4w9WgXcQ", 
                        CreatedAt = DateTime.Now, 
                        CreateBy = 1 
                    },
                    new Course 
                    { 
                        Title = "React.js - Xây dựng ứng dụng web hi�?n đại", 
                        CategoryId = 1, 
                        Price = 399000, 
                        Thumbnail = "https://via.placeholder.com/400x300/61dafb/000000?text=React.js", 
                        Description = "Khóa học React.js toàn di�?n v�>i hooks, context và các thư vi�?n ph�. biến", 
                        Status = "Published", 
                        PreviewVideo = "https://www.youtube.com/watch?v=dQw4w9WgXcQ", 
                        CreatedAt = DateTime.Now, 
                        CreateBy = 1 
                    },
                    new Course 
                    { 
                        Title = "Flutter - Phát tri�fn ứng dụng di đ�Tng đa nền tảng", 
                        CategoryId = 2, 
                        Price = 499000, 
                        Thumbnail = "https://via.placeholder.com/400x300/02569b/ffffff?text=Flutter", 
                        Description = "Học Flutter đ�f tạo ứng dụng iOS và Android v�>i m�Tt codebase", 
                        Status = "Published", 
                        PreviewVideo = "https://www.youtube.com/watch?v=dQw4w9WgXcQ", 
                        CreatedAt = DateTime.Now, 
                        CreateBy = 1 
                    }
                };

                _context.Courses.AddRange(courses);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Dữ li�?u đã được cập nhật thành công v�>i encoding đúng!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"L�-i: {ex.Message}" });
            }
        }
    }
}
