using Microsoft.AspNetCore.Mvc;

namespace ELearningWebsite.Controllers
{
    public class ErrorController : Controller
    {
        [Route("Error/{statusCode}")]
        public IActionResult HttpStatusCodeHandler(int statusCode)
        {
            switch (statusCode)
            {
                case 404:
                    // Redirect to home page for 404 errors
                    return RedirectToAction("Index", "Home");
                default:
                    return View("Error");
            }
        }

        [Route("Error")]
        public IActionResult Error()
        {
            return View();
        }
    }
}
