using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Helpers;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    public class AccountController : BaseController
    {
        public AccountController(IApiClient api) : base(api)
        {
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null, bool expired = false)
        {
            if (HttpContext.Session.IsLoggedIn())
            {
                return RedirectToRoleHome();
            }

            if (expired)
            {
                ViewData["Error"] = "Your session has ended. Please log in again.";
            }

            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            HttpContext.Session.Clear();

            var result = await Api.SendAsync<LoginResponse>(HttpMethod.Post, "api/auth/login",
                new { email = model.Email.Trim(), password = model.Password });

            if (!result.Success)
            {
                ModelState.AddModelError("", result.StatusCode == 401 ? "Invalid email or password." : result.Error);
                return View(model);
            }

            await StartSessionAsync(result.Data);

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return LocalRedirect(model.ReturnUrl);
            }

            return RedirectToRoleHome();
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (HttpContext.Session.IsLoggedIn())
            {
                return RedirectToRoleHome();
            }

            return View(new RegisterViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (model.Role != "Organiser" && model.Role != "Participant")
            {
                ModelState.AddModelError(nameof(model.Role), "Please choose Organiser or Participant.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string url = model.Role == "Organiser" ? "api/auth/register/organiser" : "api/auth/register/participant";

            var registered = await Api.ExecuteAsync(HttpMethod.Post, url, new
            {
                fullName = model.FullName.Trim(),
                email = model.Email.Trim(),
                password = model.Password,
                phoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim()
            });

            if (!registered.Success)
            {
                if (registered.StatusCode == 409)
                {
                    ModelState.AddModelError(nameof(model.Email), registered.Error);
                }
                else
                {
                    ModelState.AddModelError("", registered.Error);
                }

                return View(model);
            }

            // log the new user in straight away
            HttpContext.Session.Clear();
            var login = await Api.SendAsync<LoginResponse>(HttpMethod.Post, "api/auth/login",
                new { email = model.Email.Trim(), password = model.Password });

            if (!login.Success)
            {
                FlashSuccess("Your account was created. Please log in.");
                return RedirectToAction(nameof(Login));
            }

            await StartSessionAsync(login.Data);
            FlashSuccess("Welcome to RaceDay, " + model.FullName.Trim() + "!");
            return RedirectToRoleHome();
        }

        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            FlashSuccess("You have been logged out.");
            return RedirectToAction(nameof(Login));
        }

        [SessionAuthorize]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // keeps the token, role, name and picture in the session so every page knows who is logged in
        private async Task StartSessionAsync(LoginResponse login)
        {
            // the token has to be in the session first, because the profile calls below need it
            HttpContext.Session.SignIn(login.Token, login.Role, login.Id, "", "");

            string name = "";
            string picture = "";

            if (login.Role == "Participant")
            {
                var profile = await Api.GetAsync<ParticipantProfile>("api/participants/me");
                if (profile.Success && profile.Data != null)
                {
                    name = profile.Data.FullName;
                    picture = profile.Data.ProfilePictureUrl;
                }
            }
            else
            {
                var profile = await Api.GetAsync<OrganiserProfile>("api/organisers/me");
                if (profile.Success && profile.Data != null)
                {
                    name = profile.Data.FullName;
                }
            }

            HttpContext.Session.SetDisplayDetails(name, picture);
        }
    }
}
