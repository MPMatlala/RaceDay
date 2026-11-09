using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    // a Participant's own enrolments and their status
    [SessionAuthorize("Participant")]
    public class MyEnrolmentsController : BaseController
    {
        public MyEnrolmentsController(IApiClient api) : base(api)
        {
        }

        public async Task<IActionResult> Index()
        {
            var result = await Api.GetAsync<List<EnrolmentDto>>("api/participants/me/enrolments");
            if (!result.Success)
            {
                ViewData["Error"] = result.Error;
            }

            var enrolments = (result.Data ?? new List<EnrolmentDto>())
                .OrderByDescending(e => e.EventDate)
                .ThenBy(e => e.EventName)
                .ToList();

            return View(enrolments);
        }

        [HttpPost]
        public async Task<IActionResult> Cancel(int id)
        {
            var result = await Api.ExecuteAsync(HttpMethod.Delete, "api/enrolments/" + id);

            if (result.Success)
            {
                FlashSuccess("Your enrolment has been cancelled.");
            }
            else
            {
                FlashError(result.Error);
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
