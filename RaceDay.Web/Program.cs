using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// MVC - every POST is protected against forgery and no page is cached, so the Back button
// cannot show a protected page after logging out
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<ApiExceptionFilter>();
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    options.Filters.Add(new ResponseCacheAttribute { NoStore = true, Location = ResponseCacheLocation.None });
});

// session - keeps the token, role and name of the logged-in user between requests
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".RaceDay.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromMinutes(110); // the API token lasts 2 hours
});

// the one and only way this app gets data: the RaceDay API from Part 2
string apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"];
if (string.IsNullOrWhiteSpace(apiBaseUrl))
{
    throw new InvalidOperationException("ApiSettings:BaseUrl is not set.");
}
bool allowUntrustedCertificate = builder.Configuration.GetValue<bool>("ApiSettings:AllowUntrustedCertificate");

builder.Services
    .AddHttpClient<IApiClient, ApiClient>(client =>
    {
        client.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        var handler = new HttpClientHandler();
        if (allowUntrustedCertificate)
        {
            // only for talking to the API's https development certificate
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        }
        return handler;
    });

var app = builder.Build();

// a friendly error page instead of a stack trace, in every environment
app.UseExceptionHandler("/Home/Error");
app.UseStatusCodePagesWithReExecute("/Home/HttpStatus", "?code={0}");

// the same number and date format everywhere (a "," decimal separator would break the number inputs)
var culture = CultureInfo.InvariantCulture;
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(culture),
    SupportedCultures = new List<CultureInfo> { culture },
    SupportedUICultures = new List<CultureInfo> { culture }
});

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern:  "{controller=Home}/{action=Index}/{id?}");

app.Run();
//end of the class