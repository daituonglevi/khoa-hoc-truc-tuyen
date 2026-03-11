using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using ELearningWebsite.Data;
using ELearningWebsite.Models;

namespace ELearningWebsite.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UsersController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole<int>> _roleManager;

        public UsersController(
            ApplicationDbContext context,
            ILogger<UsersController> logger,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<int>> roleManager)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 10, string search = "")
        {
            ViewData["Title"] = "Quản lý Users";

            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u => u.FullName.Contains(search) || u.Email.Contains(search));
                ViewData["Search"] = search;
            }

            var totalUsers = await query.CountAsync();
            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Lấy roles cho từng user
            var userRoles = new Dictionary<int, IList<string>>();
            foreach (var user in users)
            {
                userRoles[user.Id] = await _userManager.GetRolesAsync(user);
            }

            // Lấy tất cả roles có sẵn
            var allRoles = await _roleManager.Roles.ToListAsync();

            var viewModel = new UsersIndexViewModel
            {
                Users = users,
                UserRoles = userRoles,
                AllRoles = allRoles,
                CurrentPage = page,
                PageSize = pageSize,
                TotalUsers = totalUsers,
                TotalPages = (int)Math.Ceiling((double)totalUsers / pageSize),
                Search = search
            };

            return View(viewModel);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus([FromForm] int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return Json(new { success = false, message = "User không tôn tại" });
                }

                user.IsVerified = !user.IsVerified;
                user.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new {
                    success = true,
                    message = $"Đã {(user.IsVerified ? "kích hoạt" : "vô hi�?u hóa")} user thành công",
                    isVerified = user.IsVerified
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling user status for user {UserId}", id);
                return Json(new { success = false, message = "Có l�-i xảy ra khi cập nhật trạng thái user" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole([FromForm] int userId, [FromForm] string roleName)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    return Json(new { success = false, message = "User không tôn tại" });
                }

                // Ki�fm tra role có tôn tại không
                var roleExists = await _roleManager.RoleExistsAsync(roleName);
                if (!roleExists)
                {
                    return Json(new { success = false, message = "Role không tôn tại" });
                }

                // Lấy roles hi�?n tại của user
                var currentRoles = await _userManager.GetRolesAsync(user);

                // Xóa tất cả roles hi�?n tại
                if (currentRoles.Any())
                {
                    var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                    if (!removeResult.Succeeded)
                    {
                        return Json(new { success = false, message = "Không th�f xóa role cũ" });
                    }
                }

                // Thêm role m�>i
                var addResult = await _userManager.AddToRoleAsync(user, roleName);
                if (!addResult.Succeeded)
                {
                    return Json(new { success = false, message = "Không th�f thêm role m�>i" });
                }

                // Cập nhật thời gian
                user.UpdatedAt = DateTime.Now;
                await _userManager.UpdateAsync(user);

                return Json(new {
                    success = true,
                    message = $"Đã cập nhật role thành {roleName} cho user {user.FullName}",
                    newRole = roleName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing role for user {UserId}", userId);
                return Json(new { success = false, message = "Có l�-i xảy ra khi cập nhật role" });
            }
        }
    }

    // ViewModels
    public class UsersIndexViewModel
    {
        public IEnumerable<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
        public Dictionary<int, IList<string>> UserRoles { get; set; } = new Dictionary<int, IList<string>>();
        public IEnumerable<IdentityRole<int>> AllRoles { get; set; } = new List<IdentityRole<int>>();
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalUsers { get; set; }
        public int TotalPages { get; set; }
        public string Search { get; set; } = string.Empty;
    }


}
