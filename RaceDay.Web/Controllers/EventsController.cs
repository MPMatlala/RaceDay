using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Helpers;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    // the Participant home page: browse upcoming events, open one, enrol
    [SessionAuthorize("Participant")]
    public class EventsController : BaseController
    {
        public EventsController(IApiClient api) : base(api)
        {
        }

        public async Task<IActionResult> Index(string keyword, string location, DateTime? date, string eventType)
        {
            // keyword, location and date are filtered by the API; the event type is filtered here
            var query = new List<string>();
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query.Add("keyword=" + Uri.EscapeDataString(keyword.Trim()));
            }
            if (!string.IsNullOrWhiteSpace(location))
            {
                query.Add("location=" + Uri.EscapeDataString(location.Trim()));
            }
            if (date.HasValue)
            {
                query.Add("date=" + date.Value.ToString("yyyy-MM-dd"));
            }

            string url = "api/events" + (query.Count > 0 ? "?" + string.Join("&", query) : "");
            var result = await Api.GetAsync<List<EventDto>>(url);
            if (!result.Success)
            {
                ViewData["Error"] = result.Error;
            }

            var events = result.Data ?? new List<EventDto>();
            if (!string.IsNullOrWhiteSpace(eventType))
            {
                events = events.Where(e => e.EventType == eventType).ToList();
            }

            // mark the events this participant has already entered
            var enrolled = new HashSet<int>();
            var mine = await Api.GetAsync<List<EnrolmentDto>>("api/participants/me/enrolments");
            if (mine.Success && mine.Data != null)
            {
                foreach (var enrolment in mine.Data.Where(e => e.Status != "Cancelled"))
                {
                    enrolled.Add(enrolment.EventId);
                }
            }

            return View(new EventBrowseViewModel
            {
                Events = events,
                EnrolledEventIds = enrolled,
                Keyword = keyword,
                Location = location,
                Date = date,
                EventType = eventType
            });
        }

        public async Task<IActionResult> Details(int id)
        {
            var result = await Api.GetAsync<EventDetailDto>("api/events/" + id);
            if (!result.Success)
            {
                FlashError(result.NotFound ? "That event could not be found." : result.Error);
                return RedirectToAction(nameof(Index));
            }

            var enrolledCategories = new HashSet<int>();
            var mine = await Api.GetAsync<List<EnrolmentDto>>("api/participants/me/enrolments");
            if (mine.Success && mine.Data != null)
            {
                foreach (var enrolment in mine.Data.Where(e => e.Status != "Cancelled" && e.EventId == id))
                {
                    enrolledCategories.Add(enrolment.CategoryId);
                }
            }

            return View(new EventDetailsViewModel { Event = result.Data, EnrolledCategoryIds = enrolledCategories });
        }

        [HttpPost]
        public async Task<IActionResult> Enrol(int eventId, int? categoryId)
        {
            if (categoryId == null)
            {
                FlashError("Please choose a category before enrolling.");
                return RedirectToAction(nameof(Details), new { id = eventId });
            }

            var result = await Api.SendAsync<EnrolResponse>(HttpMethod.Post, "api/categories/" + categoryId.Value + "/enrolments");

            if (!result.Success)
            {
                FlashError(result.Error);
                return RedirectToAction(nameof(Details), new { id = eventId });
            }

            FlashSuccess("You are in! Your race number is " + result.Data.RaceNumber + ".");
            return RedirectToAction("Index", "MyEnrolments");
        }

        // the weather box on the event page loads this in the background so a slow weather service never slows the page
        [HttpGet]
        public async Task<IActionResult> Weather(int id)
        {
            var result = await Api.GetAsync<WeatherDto>("api/events/" + id + "/weather");
            if (!result.Success || result.Data == null)
            {
                return Json(new { available = false });
            }

            return Json(new { available = true, forecast = result.Data.Forecast });
        }
    }
}
