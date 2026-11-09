using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Helpers;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    // the Organiser dashboard
    [SessionAuthorize("Organiser")]
    public class OrganiserController : BaseController
    {
        public OrganiserController(IApiClient api) : base(api)
        {
        }

        public async Task<IActionResult> Index()
        {
            var result = await Api.GetAsync<List<EventSummaryDto>>("api/organisers/me/events");
            if (!result.Success)
            {
                ViewData["Error"] = result.Error;
            }

            var events = result.Data ?? new List<EventSummaryDto>();
            var upcoming = events.Where(e => e.EventDate.Date >= DateTime.Today).OrderBy(e => e.EventDate).ToList();

            var model = new DashboardViewModel
            {
                OrganiserName = HttpContext.Session.GetFullName(),
                Events = events.OrderBy(e => e.EventDate).ToList(),
                UpcomingEvents = upcoming,
                TotalEvents = events.Count,
                UpcomingCount = upcoming.Count,
                TotalEnrolments = events.Sum(e => e.TotalEnrolments)
            };

            return View(model);
        }
    }
}
