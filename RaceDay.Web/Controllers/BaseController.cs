using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Helpers;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    // shared helpers for the controllers that talk to the API
    public abstract class BaseController : Controller
    {
        protected readonly IApiClient Api;

        protected BaseController(IApiClient api)
        {
            Api = api;
        }

        protected int CurrentUserId => HttpContext.Session.GetUserId();

        // one-off messages shown at the top of the next page
        protected void FlashSuccess(string message) { TempData["Success"] = message; }
        protected void FlashError(string message) { TempData["Error"] = message; }
        protected void FlashWarning(string message) { TempData["Warning"] = message; }

        protected IActionResult RedirectToRoleHome()
        {
            if (HttpContext.Session.GetRole() == "Organiser")
            {
                return RedirectToAction("Index", "Organiser");
            }

            return RedirectToAction("Index", "Events");
        }

        // loads an event and makes sure it belongs to the logged-in Organiser. Returns null (with a message) if not.
        protected async Task<EventDetailDto> LoadOwnedEventAsync(int eventId)
        {
            var result = await Api.GetAsync<EventDetailDto>("api/events/" + eventId);

            if (!result.Success)
            {
                FlashError(result.NotFound ? "That event could not be found." : result.Error);
                return null;
            }

            if (result.Data.OrganiserId != CurrentUserId)
            {
                FlashError("You can only manage your own events.");
                return null;
            }

            return result.Data;
        }

        // "which event?" page for Organiser screens that work on one event at a time
        protected async Task<IActionResult> ShowEventPickerAsync(string title, string intro, string controller, string action, string buttonText)
        {
            var result = await Api.GetAsync<List<EventSummaryDto>>("api/organisers/me/events");
            if (!result.Success)
            {
                ViewData["Error"] = result.Error;
            }

            var model = new EventPickerViewModel
            {
                Title = title,
                Intro = intro,
                Controller = controller,
                Action = action,
                ButtonText = buttonText,
                Events = result.Data ?? new List<EventSummaryDto>()
            };

            return View("~/Views/Shared/EventPicker.cshtml", model);
        }
    }
}
