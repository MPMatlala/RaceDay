using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Helpers;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    // a Participant views and updates their details and profile picture
    [SessionAuthorize("Participant")]
    public class ProfileController : BaseController
    {
        public ProfileController(IApiClient api) : base(api)
        {
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var result = await Api.GetAsync<ParticipantProfile>("api/participants/me");
            if (!result.Success)
            {
                FlashError(result.Error);
                return RedirectToAction("Index", "Events");
            }

            return View(ToViewModel(result.Data));
        }

        [HttpPost]
        public async Task<IActionResult> Update(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await FillReadOnlyDetailsAsync(model);
                return View("Index", model);
            }

            var result = await Api.SendAsync<ParticipantProfile>(HttpMethod.Put, "api/participants/me", new
            {
                fullName = model.FullName.Trim(),
                phoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim()
            });

            if (!result.Success)
            {
                ModelState.AddModelError("", result.Error);
                await FillReadOnlyDetailsAsync(model);
                return View("Index", model);
            }

            HttpContext.Session.SetDisplayDetails(result.Data.FullName, result.Data.ProfilePictureUrl);
            FlashSuccess("Your details have been updated.");
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> UploadPicture(IFormFile picture)
        {
            string problem = UploadValidator.Validate(picture);
            if (problem != null)
            {
                FlashError(problem);
                return RedirectToAction(nameof(Index));
            }

            var result = await Api.UploadAsync<PictureUploadResponse>("api/participants/me/picture", picture);
            if (!result.Success)
            {
                FlashError(result.Error);
                return RedirectToAction(nameof(Index));
            }

            HttpContext.Session.SetDisplayDetails(HttpContext.Session.GetFullName(), result.Data.ProfilePictureUrl);
            FlashSuccess("Your profile picture has been updated.");
            return RedirectToAction(nameof(Index));
        }

        private static ProfileViewModel ToViewModel(ParticipantProfile profile)
        {
            return new ProfileViewModel
            {
                FullName = profile.FullName,
                PhoneNumber = profile.PhoneNumber,
                Email = profile.Email,
                ProfilePictureUrl = profile.ProfilePictureUrl
            };
        }

        // the email and picture are not part of the form, so they are loaded again when the form is shown a second time
        private async Task FillReadOnlyDetailsAsync(ProfileViewModel model)
        {
            var current = await Api.GetAsync<ParticipantProfile>("api/participants/me");
            if (current.Success && current.Data != null)
            {
                model.Email = current.Data.Email;
                model.ProfilePictureUrl = current.Data.ProfilePictureUrl;
            }
        }
    }
}
