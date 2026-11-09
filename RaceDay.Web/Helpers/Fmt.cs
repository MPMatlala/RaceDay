namespace RaceDay.Web.Helpers
{
    // small formatting helpers used by the views, so every page shows things the same way
    public static class Fmt
    {
        public static string Date(DateTime date)
        {
            return date.ToString("ddd, d MMM yyyy");
        }

        public static string ShortDate(DateTime date)
        {
            return date.ToString("d MMM yyyy");
        }

        // 1:52:30
        public static string Time(TimeSpan? time)
        {
            if (time == null)
            {
                return "-";
            }

            return ((int)time.Value.TotalHours) + time.Value.ToString(@"\:mm\:ss");
        }

        // 1 -> 1st, 2 -> 2nd, 11 -> 11th, 47 -> 47th
        public static string Ordinal(int number)
        {
            int lastTwo = number % 100;
            if (lastTwo >= 11 && lastTwo <= 13)
            {
                return number + "th";
            }

            switch (number % 10)
            {
                case 1: return number + "st";
                case 2: return number + "nd";
                case 3: return number + "rd";
                default: return number + "th";
            }
        }

        public static string Money(decimal amount)
        {
            return amount == 0 ? "Free" : "R" + amount.ToString("0.00");
        }

        public static string Distance(decimal km)
        {
            return km.ToString("0.##") + " km";
        }

        // colour-coded enrolment statuses (see site.css)
        public static string StatusClass(string status)
        {
            switch (status)
            {
                case "Confirmed": return "status-confirmed";
                case "Pending": return "status-pending";
                case "Cancelled": return "status-cancelled";
                default: return "status-other";
            }
        }

        public static string TypeIcon(string eventType)
        {
            switch (eventType)
            {
                case "Walk": return "bi-person-walking";
                case "Cycle": return "bi-bicycle";
                default: return "bi-lightning-charge-fill";
            }
        }
    }
}
