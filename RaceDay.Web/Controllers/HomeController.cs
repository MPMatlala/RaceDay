using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Helpers;

namespace RaceDay.Web.Controllers
{
    public class HomeController : Controller
    {
        // the public landing page. People who are already logged in go straight to their own home page.
        public IActionResult Index()
        {
            if (HttpContext.Session.IsLoggedIn())
            {
                if (HttpContext.Session.GetRole() == "Organiser")
                {
                    return RedirectToAction("Index", "Organiser");
                }

                return RedirectToAction("Index", "Events");
            }

            return View();
        }

        // shown when something unexpected goes wrong. No technical details are ever displayed.
        [IgnoreAntiforgeryToken]
        public IActionResult Error()
        {
            return View();
        }

        // 404 and friends
        [IgnoreAntiforgeryToken]
        public IActionResult HttpStatus(int code)
        {
            ViewData["Code"] = code;
            Response.StatusCode = code;
            return View();
        }
    }
}
