namespace RaceDay.Web.Services
{
    // what the MVC controllers get back from every API call
    public class ApiResponse
    {
        public bool Success { get; set; }
        public int StatusCode { get; set; }

        // a message that is safe to show to the user
        public string Error { get; set; }

        public bool NotFound => StatusCode == 404;
    }

    public class ApiResponse<T> : ApiResponse
    {
        public T Data { get; set; }
    }

    // thrown when the API says the token is no longer valid - the filter sends the user back to the login page
    public class SessionExpiredException : Exception
    {
        public SessionExpiredException() : base("The API token has expired or is invalid.")
        {
        }
    }
}
