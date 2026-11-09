using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RaceDay.Api.Data;
using RaceDay.Api.Helpers;
using RaceDay.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// database (SQL Server + Entity Framework Core)
builder.Services.AddDbContext<RaceDayContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();
builder.Services.AddHttpClient(); // used for the weather endpoint

// where event banners and profile pictures are stored
//   Storage:Provider = "Auto" (default) -> Azure Blob Storage when AzureBlob:ConnectionString is set, otherwise the API's own "uploads" folder
//   Storage:Provider = "Azure"          -> always Azure Blob Storage
//   Storage:Provider = "Local"          -> always the "uploads" folder
builder.Services.AddHttpContextAccessor();
string storageProvider = builder.Configuration["Storage:Provider"] ?? "Auto";
bool azureConfigured = !string.IsNullOrWhiteSpace(builder.Configuration["AzureBlob:ConnectionString"]);
bool useAzure = storageProvider.Equals("Azure", StringComparison.OrdinalIgnoreCase)
    || (storageProvider.Equals("Auto", StringComparison.OrdinalIgnoreCase) && azureConfigured);
if (useAzure)
{
    builder.Services.AddSingleton<IBlobService, BlobService>();
}
else
{
    builder.Services.AddSingleton<IBlobService, LocalFileService>();
}

// authentication - the user logs in and gets a token, which keeps their login and role for every request
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
            RoleClaimType = "role"
        };
    });

builder.Services.AddAuthorization();

// swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RaceDay API",
        Version = "v1",
        Description = "API for managing running, walking and cycling events."
    });

    // empty text boxes instead of "string" placeholders
    c.SchemaFilter<EmptyExampleFilter>();

    // show the /// comments from the controllers in swagger
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // adds the Authorize button so we can paste the token
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Login first, then paste the token here."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] { }
        }
    });
});

var app = builder.Build();

app.Logger.LogInformation("Image storage: {Storage}", useAzure ? "Azure Blob Storage" : "local uploads folder");

// the API builds its own database on first run, so nobody has to run migrations or SQL scripts:
//   1. create the database and tables from the models (does nothing if they already exist)
//   2. add the two image columns if an older database is missing them
//   3. add sample data when the database is empty
// The tests use an in-memory database, so this only runs against SQL Server.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RaceDayContext>();
    try
    {
        if (db.Database.IsSqlServer())
        {
            db.Database.EnsureCreated();
            SchemaUpdater.EnsureImageColumns(db);
            DbSeeder.Seed(db);
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "The database could not be prepared. Check the DefaultConnection setting and that SQL Server LocalDB is running.");
    }
}

app.UseSwagger();
app.UseSwaggerUI();

// serves the images saved by the Local storage option
string uploadsPath = Path.Combine(app.Environment.ContentRootPath, "uploads");
Directory.CreateDirectory(uploadsPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

// in Development the MVC app (and its Docker container) calls the plain http address,
// and a redirect to https://localhost would break that
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// needed so the test project can start the API
public partial class Program { }
