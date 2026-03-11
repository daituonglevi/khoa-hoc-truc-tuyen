using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ELearningWebsite.Data;
using ELearningWebsite.Models;

namespace ELearningWebsite.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Categories
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10, string search = "", string status = "")
        {
            try
            {
                var query = _context.Categories.AsQueryable();

                // Search filter
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(c => c.Name.Contains(search) || 
                                           (c.Description != null && c.Description.Contains(search)));
                }

                // Status filter
                if (!string.IsNullOrEmpty(status) && int.TryParse(status, out int statusInt))
                {
                    query = query.Where(c => c.Status == statusInt);
                }

                var totalCategories = await query.CountAsync();
                var categories = await query
                    .OrderBy(c => c.Name)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var viewModel = new CategoriesIndexViewModel
                {
                    Categories = categories,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCategories = totalCategories,
                    TotalPages = (int)Math.Ceiling((double)totalCategories / pageSize),
                    Search = search,
                    SelectedStatus = status
                };

                ViewBag.CurrentPage = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalItems = totalCategories;
                ViewBag.TotalPages = viewModel.TotalPages;
                ViewBag.Search = search;
                ViewBag.Status = status;

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có l�-i xảy ra: " + ex.Message;
                return View(new CategoriesIndexViewModel());
            }
        }

        // GET: Admin/Categories/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var category = await _context.Categories
                    .Include(c => c.Courses)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (category == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy danh mục này";
                    return RedirectToAction(nameof(Index));
                }

                return View(category);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có l�-i xảy ra: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Admin/Categories/Create
        public IActionResult Create()
        {
            return View(new Category());
        }

        // POST: Admin/Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Check if category name already exists
                    var existingCategory = await _context.Categories
                        .FirstOrDefaultAsync(c => c.Name.ToLower() == category.Name.ToLower());

                    if (existingCategory != null)
                    {
                        ModelState.AddModelError("Name", "Tên danh mục đã tôn tại");
                        return View(category);
                    }

                    _context.Categories.Add(category);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Thêm danh mục thành công!";
                    return RedirectToAction(nameof(Index));
                }

                return View(category);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có l�-i xảy ra: " + ex.Message;
                return View(category);
            }
        }

        // GET: Admin/Categories/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var category = await _context.Categories.FindAsync(id);
                if (category == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy danh mục này";
                    return RedirectToAction(nameof(Index));
                }

                return View(category);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có l�-i xảy ra: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Category category)
        {
            if (id != category.Id)
            {
                return NotFound();
            }

            try
            {
                if (ModelState.IsValid)
                {
                    // Check if category name already exists (excluding current category)
                    var existingCategory = await _context.Categories
                        .FirstOrDefaultAsync(c => c.Name.ToLower() == category.Name.ToLower() && c.Id != id);

                    if (existingCategory != null)
                    {
                        ModelState.AddModelError("Name", "Tên danh mục đã tôn tại");
                        return View(category);
                    }

                    _context.Update(category);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Cập nhật danh mục thành công!";
                    return RedirectToAction(nameof(Index));
                }

                return View(category);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có l�-i xảy ra: " + ex.Message;
                return View(category);
            }
        }

        // POST: Admin/Categories/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var category = await _context.Categories
                    .Include(c => c.Courses)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (category == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy danh mục này" });
                }

                // Check if category has courses
                if (category.Courses.Any())
                {
                    return Json(new { success = false, message = "Không th�f xóa danh mục này vì đang có khóa học sử dụng" });
                }

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Xóa danh mục thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có l�-i xảy ra: " + ex.Message });
            }
        }

        // POST: Admin/Categories/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, int status)
        {
            try
            {
                var category = await _context.Categories.FindAsync(id);
                if (category == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy danh mục này" });
                }

                category.Status = status;
                _context.Update(category);
                await _context.SaveChangesAsync();

                string statusText = status == 1 ? "Kích hoạt" : "Vô hi�?u hóa";
                return Json(new { success = true, message = $"{statusText} danh mục thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có l�-i xảy ra: " + ex.Message });
            }
        }
    }

    // ViewModels
    public class CategoriesIndexViewModel
    {
        public List<Category> Categories { get; set; } = new();
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalCategories { get; set; }
        public int TotalPages { get; set; }
        public string? Search { get; set; }
        public string? SelectedStatus { get; set; }
    }
}
