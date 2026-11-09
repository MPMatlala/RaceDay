using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Helpers;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{//start of namespace
    // capture and publish finish times and positions once an event is over
    [SessionAuthorize("Organiser")]
    public class ManageResultsController : BaseController
    {//start of ManageResultsController
        public ManageResultsController(IApiClient api) : base(api)
        {
        }

        public async Task<IActionResult> Index(int? eventId)
        {
            if (eventId == null)
            {
                return await ShowEventPickerAsync(
                    "Results",
                    "Choose an event to capture finish times and positions.",
                    "ManageResults", "Index", "Enter results");
            }

            var ev = await LoadOwnedEventAsync(eventId.Value);
            if (ev == null)
            {
                return RedirectToAction("Index", "ManageEvents");
            }

            var enrolments = await Api.GetAsync<List<EnrolmentDto>>("api/events/" + eventId.Value + "/enrolments");
            var results = await Api.GetAsync<List<ResultDto>>("api/events/" + eventId.Value + "/results");

            if (!enrolments.Success)
            {
                ViewData["Error"] = enrolments.Error;
            }
            else if (!results.Success)
            {
                ViewData["Error"] = results.Error;
            }

            var resultList = results.Data ?? new List<ResultDto>();

            // cancelled enrolments never run, so they get no row
            var rows = (enrolments.Data ?? new List<EnrolmentDto>())
                .Where(e => e.Status != "Cancelled")
                .Select(e =>
                {
                    var result = resultList.FirstOrDefault(r => r.EnrolmentId == e.EnrolmentId);
                    return new ResultRow
                    {
                        EnrolmentId = e.EnrolmentId,
                        ResultId = result?.ResultId,
                        ParticipantName = e.ParticipantName,
                        RaceNumber = e.RaceNumber,
                        CategoryName = e.CategoryName,
                        Status = e.Status,
                        FinishTime = result?.FinishTime == null ? "" : Fmt.Time(result.FinishTime),
                        ChipTime = result?.ChipTime == null ? "" : Fmt.Time(result.ChipTime),
                        Position = result?.Position
                    };
                })
                .OrderBy(r => r.CategoryName)
                .ThenBy(r => r.Position ?? int.MaxValue)
                .ThenBy(r => r.ParticipantName)
                .ToList();

            return View(new ResultsPageViewModel
            {
                Event = ev,
                Rows = rows,
                EventHasHappened = ev.EventDate.Date <= DateTime.Today,
                CapturedCount = rows.Count(r => r.HasResult)
            });
        }

        [HttpPost]
        public async Task<IActionResult> Save(int eventId, int enrolmentId, int? resultId, string finishTime, string chipTime, int? position)
        {
            if (!TryParseTime(finishTime, out TimeSpan finish))
            {
                FlashError("Enter the finish time as hours:minutes:seconds, for example 1:52:30.");
                return RedirectToAction(nameof(Index), new { eventId = eventId });
            }

            TimeSpan? chip = null;
            if (!string.IsNullOrWhiteSpace(chipTime))
            {
                if (!TryParseTime(chipTime, out TimeSpan parsedChip))
                {
                    FlashError("Enter the chip time as hours:minutes:seconds, for example 1:52:28.");
                    return RedirectToAction(nameof(Index), new { eventId = eventId });
                }
                chip = parsedChip;
            }

            if (position == null || position < 1)
            {
                FlashError("Enter the finishing position as a whole number, 1 or higher.");
                return RedirectToAction(nameof(Index), new { eventId = eventId });
            }

            var body = new { finishTime = finish, chipTime = chip, position = position.Value };

            ApiResponse result;
            if (resultId.HasValue)
            {
                result = await Api.SendAsync<ResultDto>(HttpMethod.Put, "api/results/" + resultId.Value, body);
            }
            else
            {
                result = await Api.SendAsync<CreatedResponse>(HttpMethod.Post, "api/enrolments/" + enrolmentId + "/results", body);
            }

            if (result.Success)
            {
                FlashSuccess("Result saved and published.");
            }
            else
            {
                FlashError(result.Error);
            }

            return RedirectToAction(nameof(Index), new { eventId = eventId });
        }

        // accepts h:mm:ss (and hh:mm:ss). A plain "52:30" is rejected so it can never be read as 52 hours.
        private static bool TryParseTime(string text, out TimeSpan time)
        {
            time = TimeSpan.Zero;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            var match = Regex.Match(text.Trim(), @"^(\d{1,2}):([0-5]\d):([0-5]\d)$");
            if (!match.Success)
            {
                return false;
            }

            time = new TimeSpan(int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value), int.Parse(match.Groups[3].Value));
            return time > TimeSpan.Zero;
        }
    }
}
