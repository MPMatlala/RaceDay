using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Helpers;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    // add age or distance categories to an event
    [SessionAuthorize("Organiser")]
    public class ManageCategoriesController : BaseController
    {
        public ManageCategoriesController(IApiClient api) : base(api)
        {
        }

        public async Task<IActionResult> Index(int? eventId)
        {
            if (eventId == null)
            {
                return await ShowEventPickerAsync(
                    "Categories",
                    "Choose an event to manage its age and distance categories.",
                    "ManageCategories", "Index", "Manage categories");
            }

            var ev = await LoadOwnedEventAsync(eventId.Value);
            if (ev == null)
            {
                return RedirectToAction("Index", "ManageEvents");
            }

            return View(new CategoriesPageViewModel
            {
                Event = ev,
                Form = new CategoryFormViewModel { EventId = ev.EventId }
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create(CategoryFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var ev = await LoadOwnedEventAsync(model.EventId);
                if (ev == null)
                {
                    return RedirectToAction("Index", "ManageEvents");
                }

                return View("Index", new CategoriesPageViewModel { Event = ev, Form = model });
            }

            var result = await Api.SendAsync<CreatedResponse>(HttpMethod.Post, "api/events/" + model.EventId + "/categories", ToRequest(model));

            if (result.Success)
            {
                FlashSuccess("Category \"" + model.CategoryName.Trim() + "\" added.");
            }
            else
            {
                FlashError(result.Error);
            }

            return RedirectToAction(nameof(Index), new { eventId = model.EventId });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var category = await Api.GetAsync<CategoryDto>("api/categories/" + id);
            if (!category.Success)
            {
                FlashError(category.NotFound ? "That category could not be found." : category.Error);
                return RedirectToAction("Index", "ManageEvents");
            }

            var ev = await LoadOwnedEventAsync(category.Data.EventId);
            if (ev == null)
            {
                return RedirectToAction("Index", "ManageEvents");
            }

            ViewData["EventName"] = ev.EventName;

            return View(new CategoryFormViewModel
            {
                CategoryId = category.Data.CategoryId,
                EventId = category.Data.EventId,
                CategoryName = category.Data.CategoryName,
                DistanceKm = category.Data.DistanceKm,
                EntryFee = category.Data.EntryFee,
                MaxParticipants = category.Data.MaxParticipants
            });
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, CategoryFormViewModel model)
        {
            model.CategoryId = id;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await Api.SendAsync<CategoryDto>(HttpMethod.Put, "api/categories/" + id, ToRequest(model));
            if (!result.Success)
            {
                ModelState.AddModelError("", result.Error);
                return View(model);
            }

            FlashSuccess("Category updated.");
            return RedirectToAction(nameof(Index), new { eventId = model.EventId });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id, int eventId)
        {
            var result = await Api.ExecuteAsync(HttpMethod.Delete, "api/categories/" + id);

            if (result.Success)
            {
                FlashSuccess("Category removed.");
            }
            else
            {
                FlashError(result.Error);
            }

            return RedirectToAction(nameof(Index), new { eventId = eventId });
        }

        private object ToRequest(CategoryFormViewModel model)
        {
            return new
            {
                categoryName = model.CategoryName.Trim(),
                distanceKm = model.DistanceKm.Value,
                entryFee = model.EntryFee.Value,
                maxParticipants = model.MaxParticipants.Value
            };
        }
    }
}
