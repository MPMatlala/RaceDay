using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Helpers;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    // create, edit and delete events (including the banner image)
    [SessionAuthorize("Organiser")]
    public class ManageEventsController : BaseController
    {
        public ManageEventsController(IApiClient api) : base(api)
        {
        }

        public async Task<IActionResult> Index()
        {
            var result = await Api.GetAsync<List<EventSummaryDto>>("api/organisers/me/events");
            if (!result.Success)
            {
                ViewData["Error"] = result.Error;
            }

            return View((result.Data ?? new List<EventSummaryDto>()).OrderBy(e => e.EventDate).ToList());
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new EventFormViewModel { EventDate = DateTime.Today.AddDays(30) });
        }

        [HttpPost]
        public async Task<IActionResult> Create(EventFormViewModel model)
        {
            ValidateForm(model, true);
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var created = await Api.SendAsync<CreatedResponse>(HttpMethod.Post, "api/events", ToRequest(model));
            if (!created.Success)
            {
                ModelState.AddModelError("", created.Error);
                return View(model);
            }

            int eventId = created.Data.EventId;

            if (model.Banner != null && model.Banner.Length > 0)
            {
                var upload = await Api.UploadAsync<BannerUploadResponse>("api/events/" + eventId + "/banner", model.Banner);
                if (!upload.Success)
                {
                    FlashWarning("The event was created, but the banner could not be uploaded: " + upload.Error + " You can try again below.");
                    return RedirectToAction(nameof(Edit), new { id = eventId });
                }
            }

            FlashSuccess("Event created. Now add the categories people can enter.");
            return RedirectToAction("Index", "ManageCategories", new { eventId = eventId });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var ev = await LoadOwnedEventAsync(id);
            if (ev == null)
            {
                return RedirectToAction(nameof(Index));
            }

            return View(new EventFormViewModel
            {
                EventId = ev.EventId,
                EventName = ev.EventName,
                Description = ev.Description,
                EventDate = ev.EventDate,
                Location = ev.Location,
                Distance = ev.Distance,
                EventType = ev.EventType,
                BannerUrl = ev.BannerUrl
            });
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, EventFormViewModel model)
        {
            model.EventId = id;

            ValidateForm(model, false);
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var updated = await Api.SendAsync<EventDto>(HttpMethod.Put, "api/events/" + id, ToRequest(model));
            if (!updated.Success)
            {
                if (updated.StatusCode == 403 || updated.StatusCode == 404)
                {
                    FlashError(updated.Error);
                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError("", updated.Error);
                return View(model);
            }

            if (model.Banner != null && model.Banner.Length > 0)
            {
                var upload = await Api.UploadAsync<BannerUploadResponse>("api/events/" + id + "/banner", model.Banner);
                if (!upload.Success)
                {
                    FlashWarning("Your changes were saved, but the banner could not be uploaded: " + upload.Error);
                    return RedirectToAction(nameof(Edit), new { id = id });
                }
            }

            FlashSuccess("Event updated.");
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await Api.ExecuteAsync(HttpMethod.Delete, "api/events/" + id);

            if (result.Success)
            {
                FlashSuccess("Event deleted.");
            }
            else
            {
                FlashError(result.Error);
            }

            return RedirectToAction(nameof(Index));
        }

        private void ValidateForm(EventFormViewModel model, bool isNew)
        {
            if (isNew && model.EventDate.HasValue && model.EventDate.Value.Date < DateTime.Today)
            {
                ModelState.AddModelError(nameof(model.EventDate), "The event date cannot be in the past.");
            }

            if (model.Banner != null && model.Banner.Length > 0)
            {
                string problem = UploadValidator.Validate(model.Banner);
                if (problem != null)
                {
                    ModelState.AddModelError(nameof(model.Banner), problem);
                }
            }
        }

        // the JSON the API expects
        private object ToRequest(EventFormViewModel model)
        {
            return new
            {
                eventName = model.EventName.Trim(),
                eventDate = model.EventDate.Value.Date,
                location = model.Location.Trim(),
                description = model.Description.Trim(),
                distance = model.Distance.Value,
                eventType = model.EventType
            };
        }
    }
}
