using System.Security.Claims;

namespace RaceDay.Api.Helpers
{
    public static class ClaimsExtensions
    {
        // gets the id of the logged-in user from the token
        public static int GetUserId(this ClaimsPrincipal user)
        {
            return int.Parse(user.FindFirst("id").Value);
        }
    }
}
