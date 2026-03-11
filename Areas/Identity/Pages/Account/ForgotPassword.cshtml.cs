using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace ELearningWebsite.Areas.Identity.Pages.Account
{
	public class ForgotPasswordModel : PageModel
	{
		[BindProperty]
		public required InputModel Input { get; set; }

		public class InputModel
		{
			[Required]
			[EmailAddress]
			public required string Email { get; set; }
		}

		public void OnGet()
		{
		}

		public IActionResult OnPost()
		{
			if (!ModelState.IsValid)
			{
				return Page();
			}
			// TODO: Add logic to send reset email
			return RedirectToPage("./Login");
		}
	}
}
