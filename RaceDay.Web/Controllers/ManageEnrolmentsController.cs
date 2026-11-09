using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Helpers;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{//start of namespace
    // see everyone who has enrolled for an event
    [SessionAuthorize("Organiser")]
    public class ManageEnrolmentsController : BaseController
    {//start of ManageEnrolmentsController
        public ManageEnrolmentsController(IApiClient api) : base(api)
        {
        }

        public async Task<IActionResult> Index(int? eventId, int? categoryId)
        {
            if (eventId == null)
            {
                return await ShowEventPickerAsync(
                    "Enrolments",
                    "Choose an event to see who has enrolled.",
                    "ManageEnrolments", "Index", "View enrolments");
            }

            var ev = await LoadOwnedEventAsync(eventId.Value);
            if (ev == null)
            {
                return RedirectToAction("Index", "ManageEvents");
            }

            var result = await Api.GetAsync<List<EnrolmentDto>>("api/events/" + eventId.Value + "/enrolments");
            if (!result.Success)
            {
                ViewData["Error"] = result.Error;
            }

            var all = result.Data ?? new List<EnrolmentDto>();
            var shown = categoryId.HasValue ? all.Where(e => e.CategoryId == categoryId.Value).ToList() : all;

            return View(new EnrolmentsPageViewModel
            {
                Event = ev,
                CategoryId = categoryId,
                Enrolments = shown.OrderBy(e => e.CategoryName).ThenBy(e => e.RaceNumber).ToList(),
                ConfirmedCount = shown.Count(e => e.Status == "Confirmed"),
                PendingCount = shown.Count(e => e.Status == "Pending"),
                CancelledCount = shown.Count(e => e.Status == "Cancelled")
            });
        }
    }
}
