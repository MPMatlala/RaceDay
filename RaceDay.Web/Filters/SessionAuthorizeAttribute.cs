using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Web.Helpers;

namespace RaceDay.Web.Filters
{
    // Protects a controller or action using the data kept in the session at login.
    //   [SessionAuthorize]               - any logged-in user
    //   [SessionAuthorize("Organiser")]  - only that role
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class SessionAuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        public string Role { get; set; }

        public SessionAuthorizeAttribute()
        {
        }

        public SessionAuthorizeAttribute(string role)
        {
            Role = role;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var session = context.HttpContext.Session;

            if (!session.IsLoggedIn())
            {
                var request = context.HttpContext.Request;
                string returnUrl = request.Method == "GET" ? request.Path.Value + request.QueryString.Value : null;

                context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl = returnUrl });
                return;
            }

            if (Role != null && session.GetRole() != Role)
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
            }
        }
    }
}
