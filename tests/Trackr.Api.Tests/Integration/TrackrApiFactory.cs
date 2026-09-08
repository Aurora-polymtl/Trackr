using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Trackr.Api.Data;

namespace Trackr.Api.Tests.Integration;

public class TrackrApiFactory : WebApplicationFactory<Program>
{
    public Action<IServiceCollection>? ConfigureTestServices { get; init; }
    
    private readonly string _databaseName =
        $"TrackrIntegrationTests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<
                IDbContextOptionsConfiguration<TrackrDbContext>>();

            services.RemoveAll<
                DbContextOptions<TrackrDbContext>>();

            services.RemoveAll<TrackrDbContext>();

            services.AddDbContext<TrackrDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
            });
            
            ConfigureTestServices?.Invoke(services);
        });
    }
}