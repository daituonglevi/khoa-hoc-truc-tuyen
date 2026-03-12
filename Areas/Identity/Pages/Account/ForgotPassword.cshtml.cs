using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using ELearningWebsite.Models;
using ELearningWebsite.Services;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authorization;

namespace ELearningWebsite.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
	public class ForgotPasswordModel : PageModel
	{
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;

        public ForgotPasswordModel(UserManager<ApplicationUser> userManager, IEmailSender emailSender)
        {
            _userManager = userManager;
            _emailSender = emailSender;
        }

		[BindProperty]
		public InputModel Input { get; set; } = new InputModel();

		public class InputModel
		{
			[Required(ErrorMessage = "Vui lòng nhập email")]
			[EmailAddress(ErrorMessage = "Email không hợp lệ")]
			public string Email { get; set; } = string.Empty;
		}

		public void OnGet()
		{
		}

		public async Task<IActionResult> OnPostAsync()
		{
			if (!ModelState.IsValid)
			{
				return Page();
			}

            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToPage("./ForgotPasswordConfirmation");
            }

            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = Url.Page(
                "/Account/ResetPassword",
                pageHandler: null,
                values: new { area = "Identity", code },
                protocol: Request.Scheme);

            await _emailSender.SendEmailAsync(
                Input.Email,
                "Lấy lại mật khẩu",
                $"Vui lòng đặt lại mật khẩu của bạn bằng cách <a href='{HtmlEncoder.Default.Encode(callbackUrl!)}'>nhấn vào đây</a>.");

			return RedirectToPage("./ForgotPasswordConfirmation");
		}
	}
}
