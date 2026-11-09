using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;
using RaceDay.Api.Services;

namespace RaceDay.Tests
{
    // starts the real API for the tests, but uses an in-memory database instead of SQL Server
    public class RaceDayFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbName = Guid.NewGuid().ToString();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                var oldOptions = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<RaceDayContext>));
                if (oldOptions != null)
                {
                    services.Remove(oldOptions);
                }

                services.AddDbContext<RaceDayContext>(options => options.UseInMemoryDatabase(_dbName));

                // never talk to Azure from the tests - use a fake that just returns a URL
                var oldBlob = services.SingleOrDefault(d => d.ServiceType == typeof(IBlobService));
                if (oldBlob != null)
                {
                    services.Remove(oldBlob);
                }

                services.AddSingleton<IBlobService, FakeBlobService>();
            });
        }
    }

    public class FakeBlobService : IBlobService
    {
        public Task<string> UploadAsync(string folder, IFormFile file)
        {
            string url = "https://fake.blob.test/raceday-images/" + folder + "/" + Guid.NewGuid().ToString("N") + Path.GetExtension(file.FileName);
            return Task.FromResult(url);
        }

        public Task DeleteAsync(string url)
        {
            return Task.CompletedTask;
        }
    }
}
