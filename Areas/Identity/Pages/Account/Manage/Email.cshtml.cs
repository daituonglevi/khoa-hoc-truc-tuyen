using System.ComponentModel.DataAnnotations;
using ELearningWebsite.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ELearningWebsite.Areas.Identity.Pages.Account.Manage;

public class EmailModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public EmailModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [EmailAddress]
    [Display(Name = "Email hiện tại")]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public bool IsEmailConfirmed { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public class InputModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email mới")]
        public string NewEmail { get; set; } = string.Empty;
    }

    private async Task LoadAsync(ApplicationUser user)
    {
        Email = await _userManager.GetEmailAsync(user) ?? string.Empty;
        IsEmailConfirmed = await _userManager.IsEmailConfirmedAsync(user);
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Không thể tải user với ID '{_userManager.GetUserId(User)}'.");
        }

        await LoadAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostChangeEmailAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Không thể tải user với ID '{_userManager.GetUserId(User)}'.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync(user);
            return Page();
        }

        var email = await _userManager.GetEmailAsync(user);
        if (Input.NewEmail != email)
        {
            var setEmailResult = await _userManager.SetEmailAsync(user, Input.NewEmail);
            if (!setEmailResult.Succeeded)
            {
                foreach (var error in setEmailResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                await LoadAsync(user);
                return Page();
            }

            var setUserNameResult = await _userManager.SetUserNameAsync(user, Input.NewEmail);
            if (!setUserNameResult.Succeeded)
            {
                foreach (var error in setUserNameResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                await LoadAsync(user);
                return Page();
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Email đã được cập nhật.";
            return RedirectToPage();
        }

        StatusMessage = "Email mới trùng với email hiện tại.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSendVerificationEmailAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Không thể tải user với ID '{_userManager.GetUserId(User)}'.");
        }

        await LoadAsync(user);
        StatusMessage = "Tính năng gửi email xác thực đang được tắt trong bản demo.";
        return RedirectToPage();
    }
}

