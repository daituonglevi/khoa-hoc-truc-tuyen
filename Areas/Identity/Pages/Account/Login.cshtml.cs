using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ELearningWebsite.Models;

namespace ELearningWebsite.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public IList<AuthenticationScheme> ExternalLogins { get; set; } = new List<AuthenticationScheme>();

        public string? ReturnUrl { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Email là bắt bu�Tc")]
            [EmailAddress(ErrorMessage = "Email không hợp l�?")]
            public string Email { get; set; } = "";

            [Required(ErrorMessage = "Mật khẩu là bắt bu�Tc")]
            [DataType(DataType.Password)]
            public string Password { get; set; } = "";

            [Display(Name = "Ghi nh�> đ�fng nhập")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string? returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");

                    // Ki�fm tra role và chuy�fn hư�>ng phù hợp
                    var user = await _userManager.FindByEmailAsync(Input.Email);
                    if (user != null)
                    {
                        var roles = await _userManager.GetRolesAsync(user);

                        // Nếu là Admin, chuy�fn hư�>ng đến Admin Dashboard
                        if (roles.Contains("Admin"))
                        {
                            _logger.LogInformation("Admin user logged in, redirecting to admin dashboard.");
                            return LocalRedirect("/Admin");
                        }

                        // Nếu là Instructor, chuyển đến khu vực quản lý khóa học
                        if (roles.Contains("Instructor"))
                        {
                            _logger.LogInformation("Instructor user logged in, redirecting to instructor courses.");
                            return LocalRedirect("/Admin/Courses");
                        }

                        // Nếu là Student hoặc role khác, chuy�fn về User Dashboard
                        if (roles.Contains("Student") || roles.Any())
                        {
                            _logger.LogInformation("Student user logged in, redirecting to user dashboard.");
                            return LocalRedirect("/User/Dashboard");
                        }
                    }

                    // Fallback về returnUrl hoặc trang chủ
                    return LocalRedirect(returnUrl);
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    return RedirectToPage("./Lockout");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng.");
                    return Page();
                }
            }

            return Page();
        }
    }
}
