using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Web.Services;

namespace RaceDay.Web.Filters
{
    // when the API token has expired, end the session and send the user to the login page
    public class ApiExceptionFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            if (context.Exception is SessionExpiredException)
            {
                context.HttpContext.Session.Clear();
                context.Result = new RedirectToActionResult("Login", "Account", new { expired = true });
                context.ExceptionHandled = true;
            }
        }
    }
}
