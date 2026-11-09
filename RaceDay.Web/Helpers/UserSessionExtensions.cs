namespace RaceDay.Web.Helpers
{
    // everything the app remembers about the logged-in user lives in the session
    public static class UserSessionExtensions
    {
        public static bool IsLoggedIn(this ISession session)
        {
            return !string.IsNullOrEmpty(session.GetString("Token"));
        }

        public static string GetRole(this ISession session)
        {
            return session.GetString("Role");
        }

        public static int GetUserId(this ISession session)
        {
            return session.GetInt32("UserId") ?? 0;
        }

        public static string GetFullName(this ISession session)
        {
            return session.GetString("FullName") ?? "";
        }

        public static string GetPicture(this ISession session)
        {
            return session.GetString("Picture") ?? "";
        }

        public static void SignIn(this ISession session, string token, string role, int userId, string fullName, string pictureUrl)
        {
            session.SetString("Token", token ?? "");
            session.SetString("Role", role ?? "");
            session.SetInt32("UserId", userId);
            session.SetString("FullName", fullName ?? "");
            session.SetString("Picture", pictureUrl ?? "");
        }

        public static void SetDisplayDetails(this ISession session, string fullName, string pictureUrl)
        {
            session.SetString("FullName", fullName ?? "");
            session.SetString("Picture", pictureUrl ?? "");
        }
    }
}
