using Microsoft.AspNetCore.Mvc;

namespace ELearningWebsite.Controllers
{
    public class ErrorController : Controller
    {
        [Route("Error/{statusCode}")]
        public IActionResult HttpStatusCodeHandler(int statusCode)
        {
            Response.StatusCode = statusCode;

            switch (statusCode)
            {
                case 404:
                    return View("Error");
                case 403:
                    return View("Error");
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
