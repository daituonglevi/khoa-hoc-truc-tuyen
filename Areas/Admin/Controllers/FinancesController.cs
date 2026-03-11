using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ELearningWebsite.Data;
using ELearningWebsite.Models;

namespace ELearningWebsite.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class FinancesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FinancesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Finances
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10, int? year = null, int? month = null, string type = "")
        {
            try
            {
                var query = _context.Finances.AsQueryable();

                // Year filter
                if (year.HasValue)
                {
                    query = query.Where(f => f.Year == year.Value);
                }
                else
                {
                    // Default to current year
                    year = DateTime.Now.Year;
                    query = query.Where(f => f.Year == year.Value);
                }

                // Month filter
                if (month.HasValue)
                {
                    query = query.Where(f => f.Month == month.Value);
                }

                // Type filter
                if (!string.IsNullOrEmpty(type))
                {
                    query = query.Where(f => f.Type != null && f.Type.Contains(type));
                }

                var totalRecords = await query.CountAsync();
                var finances = await query
                    .OrderByDescending(f => f.Year)
                    .ThenByDescending(f => f.Month)
                    .ThenByDescending(f => f.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Calculate statistics
                var totalRevenue = await query.SumAsync(f => f.Revenue);
                var totalFee = await query.SumAsync(f => f.Fee);
                var netRevenue = totalRevenue - totalFee;

                // Monthly statistics for current year
                var monthlyStats = await _context.Finances
                    .Where(f => f.Year == year.Value)
                    .GroupBy(f => f.Month)
                    .Select(g => new MonthlyFinanceStatistic
                    {
                        Month = g.Key,
                        Revenue = g.Sum(f => f.Revenue),
                        Fee = g.Sum(f => f.Fee),
                        Count = g.Count()
                    })
                    .OrderBy(s => s.Month)
                    .ToListAsync();

                var viewModel = new FinancesIndexViewModel
                {
                    Finances = finances,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalRecords = totalRecords,
                    TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize),
                    SelectedYear = year,
                    SelectedMonth = month,
                    SelectedType = type,
                    TotalRevenue = totalRevenue,
                    TotalFee = totalFee,
                    NetRevenue = netRevenue,
                    MonthlyStats = monthlyStats
                };

                ViewBag.CurrentPage = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalItems = totalRecords;
                ViewBag.TotalPages = viewModel.TotalPages;
                ViewBag.Year = year;
                ViewBag.Month = month;
                ViewBag.Type = type;

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có l�-i xảy ra: " + ex.Message;
                return View(new FinancesIndexViewModel());
            }
        }

        // GET: Admin/Finances/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var finance = await _context.Finances.FindAsync(id);
                if (finance == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy bản ghi tài chính này";
                    return RedirectToAction(nameof(Index));
                }

                return View(finance);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có l�-i xảy ra: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Admin/Finances/Statistics
        public async Task<IActionResult> Statistics()
        {
            try
            {
                var currentYear = DateTime.Now.Year;
                
                // Revenue by year (last 5 years)
                var yearlyStats = await _context.Finances
                    .Where(f => f.Year >= currentYear - 4)
                    .GroupBy(f => f.Year)
                    .Select(g => new YearlyFinanceStatistic
                    {
                        Year = g.Key,
                        Revenue = g.Sum(f => f.Revenue),
                        Fee = g.Sum(f => f.Fee),
                        Count = g.Count()
                    })
                    .OrderBy(s => s.Year)
                    .ToListAsync();

                // Monthly stats for current year
                var monthlyStats = await _context.Finances
                    .Where(f => f.Year == currentYear)
                    .GroupBy(f => f.Month)
                    .Select(g => new MonthlyFinanceStatistic
                    {
                        Month = g.Key,
                        Revenue = g.Sum(f => f.Revenue),
                        Fee = g.Sum(f => f.Fee),
                        Count = g.Count()
                    })
                    .OrderBy(s => s.Month)
                    .ToListAsync();

                // Revenue by type
                var typeStats = await _context.Finances
                    .Where(f => f.Year == currentYear && !string.IsNullOrEmpty(f.Type))
                    .GroupBy(f => f.Type)
                    .Select(g => new TypeFinanceStatistic
                    {
                        Type = g.Key!,
                        Revenue = g.Sum(f => f.Revenue),
                        Fee = g.Sum(f => f.Fee),
                        Count = g.Count()
                    })
                    .OrderByDescending(s => s.Revenue)
                    .ToListAsync();

                // Total statistics
                var totalRevenue = await _context.Finances.SumAsync(f => f.Revenue);
                var totalFee = await _context.Finances.SumAsync(f => f.Fee);
                var totalRecords = await _context.Finances.CountAsync();

                ViewBag.YearlyStats = yearlyStats;
                ViewBag.MonthlyStats = monthlyStats;
                ViewBag.TypeStats = typeStats;
                ViewBag.TotalRevenue = totalRevenue;
                ViewBag.TotalFee = totalFee;
                ViewBag.NetRevenue = totalRevenue - totalFee;
                ViewBag.TotalRecords = totalRecords;
                ViewBag.CurrentYear = currentYear;

                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có l�-i xảy ra: " + ex.Message;
                return View();
            }
        }

        // GET: Admin/Finances/Create
        public IActionResult Create()
        {
            var finance = new Finance
            {
                Month = DateTime.Now.Month,
                Year = DateTime.Now.Year
            };
            return View(finance);
        }

        // POST: Admin/Finances/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Finance finance)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    finance.CreatedAt = DateTime.Now;
                    finance.UpdatedAt = DateTime.Now;
                    finance.CreatedBy = User.Identity?.Name ?? "Admin";
                    finance.UpdatedBy = User.Identity?.Name ?? "Admin";

                    _context.Finances.Add(finance);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Thêm bản ghi tài chính thành công!";
                    return RedirectToAction(nameof(Index));
                }

                return View(finance);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có l�-i xảy ra: " + ex.Message;
                return View(finance);
            }
        }

        // GET: Admin/Finances/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var finance = await _context.Finances.FindAsync(id);
                if (finance == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy bản ghi tài chính này";
                    return RedirectToAction(nameof(Index));
                }

                return View(finance);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có l�-i xảy ra: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Finances/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Finance finance)
        {
            if (id != finance.Id)
            {
                return NotFound();
            }

            try
            {
                if (ModelState.IsValid)
                {
                    finance.UpdatedAt = DateTime.Now;
                    finance.UpdatedBy = User.Identity?.Name ?? "Admin";

                    _context.Update(finance);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Cập nhật bản ghi tài chính thành công!";
                    return RedirectToAction(nameof(Index));
                }

                return View(finance);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có l�-i xảy ra: " + ex.Message;
                return View(finance);
            }
        }

        // POST: Admin/Finances/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var finance = await _context.Finances.FindAsync(id);
                if (finance == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy bản ghi tài chính này" });
                }

                _context.Finances.Remove(finance);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Xóa bản ghi tài chính thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có l�-i xảy ra: " + ex.Message });
            }
        }
    }

    // ViewModels
    public class FinancesIndexViewModel
    {
        public List<Finance> Finances { get; set; } = new();
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }
        public int? SelectedYear { get; set; }
        public int? SelectedMonth { get; set; }
        public string? SelectedType { get; set; }
        public double TotalRevenue { get; set; }
        public double TotalFee { get; set; }
        public double NetRevenue { get; set; }
        public List<MonthlyFinanceStatistic> MonthlyStats { get; set; } = new();
    }

    public class MonthlyFinanceStatistic
    {
        public int Month { get; set; }
        public double Revenue { get; set; }
        public double Fee { get; set; }
        public int Count { get; set; }
    }

    public class YearlyFinanceStatistic
    {
        public int Year { get; set; }
        public double Revenue { get; set; }
        public double Fee { get; set; }
        public int Count { get; set; }
    }

    public class TypeFinanceStatistic
    {
        public string Type { get; set; } = string.Empty;
        public double Revenue { get; set; }
        public double Fee { get; set; }
        public int Count { get; set; }
    }
}
