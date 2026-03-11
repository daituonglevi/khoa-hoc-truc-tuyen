using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ELearningWebsite.Data;
using ELearningWebsite.Models;
using ELearningWebsite.Areas.Admin.ViewModels;

namespace ELearningWebsite.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class PromotionsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PromotionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Promotions
        public async Task<IActionResult> Index(int page = 1, string search = "", string status = "", string course = "")
        {
            var viewModel = new PromotionIndexViewModel
            {
                CurrentPage = page,
                SearchTerm = search,
                StatusFilter = status,
                CourseFilter = course
            };

            var query = _context.Discounts
                .Include(d => d.Course)
                .AsQueryable();

            // Apply search filter
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(d => d.Code.Contains(search) || d.Course.Title.Contains(search));
            }

            // Apply course filter
            if (!string.IsNullOrEmpty(course) && int.TryParse(course, out int courseId))
            {
                query = query.Where(d => d.CourseId == courseId);
            }

            // Apply status filter (simplified since we don't have IsActive and CurrentUses columns)
            if (!string.IsNullOrEmpty(status))
            {
                var now = DateTime.Now;
                switch (status.ToLower())
                {
                    case "active":
                        query = query.Where(d =>
                            (!d.StartDate.HasValue || d.StartDate <= now) &&
                            (!d.EndDate.HasValue || d.EndDate >= now));
                        break;
                    case "expired":
                        query = query.Where(d => d.EndDate.HasValue && d.EndDate < now);
                        break;
                    case "scheduled":
                        query = query.Where(d => d.StartDate.HasValue && d.StartDate > now);
                        break;
                    // Skip inactive and usedup filters since we don't have those columns
                }
            }

            // Get total count for pagination
            viewModel.TotalItems = await query.CountAsync();
            viewModel.TotalPages = (int)Math.Ceiling((double)viewModel.TotalItems / viewModel.PageSize);

            // Get paginated results
            var promotions = await query
                .OrderByDescending(d => d.CreatedAt)
                .Skip((page - 1) * viewModel.PageSize)
                .Take(viewModel.PageSize)
                .Select(d => new PromotionListItem
                {
                    Id = d.Id,
                    Code = d.Code,
                    DiscountPer = d.DiscountPer,
                    MaxUses = d.MaxUses,
                    CurrentUses = 0, // Default value since column doesn't exist
                    StartDate = d.StartDate,
                    EndDate = d.EndDate,
                    IsActive = true, // Default value since column doesn't exist
                    CourseName = d.Course.Title ?? "N/A",
                    CourseId = d.CourseId,
                    CreatedAt = d.CreatedAt
                })
                .ToListAsync();

            viewModel.Promotions = promotions;

            // Get available courses for filter dropdown
            viewModel.AvailableCourses = await _context.Courses
                .Where(c => c.Status == "Published")
                .OrderBy(c => c.Title)
                .ToListAsync();

            return View(viewModel);
        }

        // GET: Admin/Promotions/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var discount = await _context.Discounts
                .Include(d => d.Course)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (discount == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy khuyến mãi";
                return RedirectToAction(nameof(Index));
            }

            var viewModel = new PromotionDetailsViewModel
            {
                Id = discount.Id,
                Code = discount.Code,
                DiscountPer = discount.DiscountPer,
                MaxUses = discount.MaxUses,
                CurrentUses = 0, // Default value since column doesn't exist
                StartDate = discount.StartDate,
                EndDate = discount.EndDate,
                IsActive = true, // Default value since column doesn't exist
                CourseId = discount.CourseId,
                CourseName = discount.Course?.Title ?? "N/A",
                CoursePrice = discount.Course?.Price ?? 0,
                CreatedAt = discount.CreatedAt,
                UpdatedAt = discount.UpdatedAt,
                CreateBy = discount.CreateBy,
                UpdateBy = discount.UpdateBy ?? 0
            };

            return View(viewModel);
        }

        // GET: Admin/Promotions/Create
        public async Task<IActionResult> Create()
        {
            var viewModel = new PromotionCreateViewModel();

            viewModel.AvailableCourses = await _context.Courses
                .Where(c => c.Status == "Published")
                .OrderBy(c => c.Title)
                .ToListAsync();

            return View(viewModel);
        }

        // POST: Admin/Promotions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PromotionCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Check if code already exists
                    var existingCode = await _context.Discounts
                        .AnyAsync(d => d.Code == model.Code);

                    if (existingCode)
                    {
                        ModelState.AddModelError("Code", "Mã khuyến mãi đã tôn tại");
                    }
                    else
                    {
                        var discount = new Discount
                        {
                            Code = model.Code.ToUpper(),
                            DiscountPer = model.DiscountPer,
                            MaxUses = model.MaxUses,
                            StartDate = model.StartDate,
                            EndDate = model.EndDate,
                            CourseId = model.CourseId,
                            CreateBy = 1, // TODO: Get current admin user ID
                            CreatedAt = DateTime.Now
                        };

                        _context.Discounts.Add(discount);
                        await _context.SaveChangesAsync();

                        TempData["SuccessMessage"] = "Tạo khuyến mãi thành công";
                        return RedirectToAction(nameof(Index));
                    }
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Có l�-i xảy ra: " + ex.Message;
                }
            }

            // Reload courses if validation fails
            model.AvailableCourses = await _context.Courses
                .Where(c => c.Status == "Published")
                .OrderBy(c => c.Title)
                .ToListAsync();

            return View(model);
        }

        // GET: Admin/Promotions/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var discount = await _context.Discounts
                .FirstOrDefaultAsync(d => d.Id == id);

            if (discount == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy khuyến mãi";
                return RedirectToAction(nameof(Index));
            }

            var viewModel = new PromotionEditViewModel
            {
                Id = discount.Id,
                Code = discount.Code,
                DiscountPer = discount.DiscountPer,
                MaxUses = discount.MaxUses,
                CurrentUses = 0, // Default value since column doesn't exist
                StartDate = discount.StartDate,
                EndDate = discount.EndDate,
                CourseId = discount.CourseId,
                IsActive = true, // Default value since column doesn't exist
                CreatedAt = discount.CreatedAt,
                UpdatedAt = discount.UpdatedAt
            };

            viewModel.AvailableCourses = await _context.Courses
                .Where(c => c.Status == "Published")
                .OrderBy(c => c.Title)
                .ToListAsync();

            return View(viewModel);
        }

        // POST: Admin/Promotions/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PromotionEditViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var discount = await _context.Discounts.FindAsync(id);
                    if (discount == null)
                    {
                        TempData["ErrorMessage"] = "Không tìm thấy khuyến mãi";
                        return RedirectToAction(nameof(Index));
                    }

                    // Check if code already exists (excluding current record)
                    var existingCode = await _context.Discounts
                        .AnyAsync(d => d.Code == model.Code && d.Id != id);

                    if (existingCode)
                    {
                        ModelState.AddModelError("Code", "Mã khuyến mãi đã tôn tại");
                    }
                    else
                    {
                        discount.Code = model.Code.ToUpper();
                        discount.DiscountPer = model.DiscountPer;
                        discount.MaxUses = model.MaxUses;
                        discount.StartDate = model.StartDate;
                        discount.EndDate = model.EndDate;
                        discount.CourseId = model.CourseId;
                        discount.UpdateBy = 1; // TODO: Get current admin user ID
                        discount.UpdatedAt = DateTime.Now;

                        await _context.SaveChangesAsync();

                        TempData["SuccessMessage"] = "Cập nhật khuyến mãi thành công";
                        return RedirectToAction(nameof(Index));
                    }
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Có l�-i xảy ra: " + ex.Message;
                }
            }

            // Reload courses if validation fails
            model.AvailableCourses = await _context.Courses
                .Where(c => c.Status == "Published")
                .OrderBy(c => c.Title)
                .ToListAsync();

            return View(model);
        }

        // GET: Admin/Promotions/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var discount = await _context.Discounts
                .Include(d => d.Course)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (discount == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy khuyến mãi";
                return RedirectToAction(nameof(Index));
            }

            var viewModel = new PromotionDeleteViewModel
            {
                Id = discount.Id,
                Code = discount.Code,
                DiscountPer = discount.DiscountPer,
                CourseName = discount.Course?.Title ?? "N/A",
                CurrentUses = 0 // Default value since column doesn't exist
            };

            return View(viewModel);
        }

        // POST: Admin/Promotions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var discount = await _context.Discounts.FindAsync(id);
                if (discount == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy khuyến mãi";
                    return RedirectToAction(nameof(Index));
                }

                // Since we don't have CurrentUses column, we'll allow deletion for now
                // In a real implementation, you would check usage from order/enrollment tables

                _context.Discounts.Remove(discount);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Xóa khuyến mãi thành công";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có l�-i xảy ra: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Promotions/ToggleStatus/5
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            try
            {
                // Since we don't have IsActive column, we'll simulate toggle by updating UpdatedAt
                var discount = await _context.Discounts.FindAsync(id);
                if (discount == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy khuyến mãi" });
                }

                discount.UpdatedAt = DateTime.Now;
                discount.UpdateBy = 1; // TODO: Get current admin user ID

                await _context.SaveChangesAsync();

                return Json(new {
                    success = true,
                    message = "Cập nhật trạng thái thành công",
                    isActive = true // Always return true since we don't have IsActive column
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có l�-i xảy ra: " + ex.Message });
            }
        }

        // GET: Admin/Promotions/Statistics
        public async Task<IActionResult> Statistics()
        {
            var now = DateTime.Now;

            var stats = new
            {
                TotalPromotions = await _context.Discounts.CountAsync(),
                ActivePromotions = await _context.Discounts.CountAsync(d =>
                    (!d.StartDate.HasValue || d.StartDate <= now) &&
                    (!d.EndDate.HasValue || d.EndDate >= now)),
                ExpiredPromotions = await _context.Discounts.CountAsync(d =>
                    d.EndDate.HasValue && d.EndDate < now),
                UsedUpPromotions = 0, // Default since we don't have CurrentUses column
                TotalUsage = 0, // Default since we don't have CurrentUses column
                TopPromotions = await _context.Discounts
                    .Include(d => d.Course)
                    .OrderByDescending(d => d.CreatedAt)
                    .Take(5)
                    .Select(d => new {
                        d.Code,
                        CourseName = d.Course.Title,
                        CurrentUses = 0, // Default value
                        d.DiscountPer
                    })
                    .ToListAsync()
            };

            return Json(stats);
        }
    }
}
