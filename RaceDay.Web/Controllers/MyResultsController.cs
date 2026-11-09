using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{//start of MyResultsController
    // a Participant's race history
    [SessionAuthorize("Participant")]
    public class MyResultsController : BaseController
    {//start of MyResultsController constructor
        public MyResultsController(IApiClient api) : base(api)
        {
        }

        public async Task<IActionResult> Index()
        {
            var result = await Api.GetAsync<List<ResultDto>>("api/participants/me/results");
            if (!result.Success)
            {
                ViewData["Error"] = result.Error;
            }

            var results = (result.Data ?? new List<ResultDto>())
                .OrderByDescending(r => r.EventDate)
                .ToList();

            return View(results);
        }//end of MyResultsController constructor
    }//end of MyResultsController
}//end of namespace
