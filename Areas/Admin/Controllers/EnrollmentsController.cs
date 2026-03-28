using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ELearningWebsite.Data;
using ELearningWebsite.Models;
using ELearningWebsite.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace ELearningWebsite.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Instructor")]
    public class EnrollmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public EnrollmentsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Admin/Enrollments
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10, string search = "", string status = "")
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var query = _context.Enrollments
                    .Include(e => e.Course)
                    .AsQueryable();

                if (!IsAdmin())
                {
                    if (!currentUserId.HasValue)
                    {
                        return Forbid();
                    }

                    query = query.Where(e => e.Course.CreateBy == currentUserId.Value);
                }

                // Search filter
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(e =>
                        e.Course.Title.Contains(search) ||
                        e.UserId.ToString().Contains(search));
                }

                // Status filter
                if (!string.IsNullOrEmpty(status) && int.TryParse(status, out int statusInt))
                {
                    query = query.Where(e => e.Status == statusInt);
                }

                // Pagination
                var totalItems = await query.CountAsync();
                var enrollments = await query
                    .OrderByDescending(e => e.EnrollmentDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                ViewBag.CurrentPage = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalItems = totalItems;
                ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
                ViewBag.Search = search;
                ViewBag.Status = status;

                return View(enrollments);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có l�-i xảy ra: " + ex.Message;
                return View(new List<Enrollment>());
            }
        }

        // GET: Admin/Enrollments/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                if (!await CanManageEnrollmentAsync(id))
                {
                    return Forbid();
                }

                var enrollment = await _context.Enrollments
                    .Include(e => e.Course)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (enrollment == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đ�fng ký này";
                    return RedirectToAction(nameof(Index));
                }

                return View(enrollment);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có l�-i xảy ra: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Enrollments/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, int status)
        {
            try
            {
                if (!await CanManageEnrollmentAsync(id))
                {
                    return Json(new { success = false, message = "Bạn không có quyền cập nhật đăng ký này" });
                }

                var enrollment = await _context.Enrollments.FindAsync(id);
                if (enrollment == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đ�fng ký" });
                }

                enrollment.Status = status;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Cập nhật trạng thái thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có l�-i xảy ra: " + ex.Message });
            }
        }

        // POST: Admin/Enrollments/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (!await CanManageEnrollmentAsync(id))
                {
                    TempData["ErrorMessage"] = "Bạn không có quyền xóa đăng ký này";
                    return RedirectToAction(nameof(Index));
                }

                var enrollment = await _context.Enrollments.FindAsync(id);
                if (enrollment == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đ�fng ký này";
                    return RedirectToAction(nameof(Index));
                }

                _context.Enrollments.Remove(enrollment);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Xóa đ�fng ký thành công";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có l�-i xảy ra: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Admin/Enrollments/Statistics
        public async Task<IActionResult> Statistics()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var query = _context.Enrollments.AsQueryable();
                if (!IsAdmin())
                {
                    if (!currentUserId.HasValue)
                    {
                        return Forbid();
                    }

                    query = query.Where(e => e.Course.CreateBy == currentUserId.Value);
                }

                var totalEnrollments = await query.CountAsync();
                var activeEnrollments = await query.CountAsync(e => e.Status == 1); // Active
                var completedEnrollments = await query.CountAsync(e => e.Status == 3); // Completed
                var suspendedEnrollments = await query.CountAsync(e => e.Status == 2); // Suspended

                var monthlyEnrollments = await query
                    .Where(e => e.EnrollmentDate >= DateTime.Now.AddMonths(-12))
                    .GroupBy(e => new { e.EnrollmentDate.Year, e.EnrollmentDate.Month })
                    .Select(g => new {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Count = g.Count()
                    })
                    .OrderBy(x => x.Year).ThenBy(x => x.Month)
                    .ToListAsync();

                ViewBag.TotalEnrollments = totalEnrollments;
                ViewBag.ActiveEnrollments = activeEnrollments;
                ViewBag.CompletedEnrollments = completedEnrollments;
                ViewBag.ExpiredEnrollments = suspendedEnrollments; // Renamed for consistency
                ViewBag.MonthlyEnrollments = monthlyEnrollments;

                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có l�-i xảy ra: " + ex.Message;
                return View();
            }
        }

        private int? GetCurrentUserId()
        {
            var rawUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(rawUserId, out var userId) ? userId : null;
        }

        private bool IsAdmin()
        {
            return User.IsInRole("Admin");
        }

        private async Task<bool> CanManageEnrollmentAsync(int enrollmentId)
        {
            if (IsAdmin())
            {
                return true;
            }

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return false;
            }

            return await _context.Enrollments
                .AnyAsync(e => e.Id == enrollmentId && e.Course.CreateBy == currentUserId.Value);
        }
    }
}
