using System.Data.Common;
using DotNet.Testcontainers.Builders;
using EmailService.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;

namespace EmailService.Tests.Setup;

public class IntegrationTestWebFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public IntegrationTestWebFactory() : base()
    {
        
    }

    private readonly MsSqlContainer _dbContainer =
        new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPortBinding(1433, 1433)
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithPassword("IntegrationTestPassWord_123!")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(1433))
            .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            var descriptorType = typeof(DbContextOptions<EmailAuthEntityDbContext>);
            var descriptor = services.SingleOrDefault(s => s.ServiceType == descriptorType);
            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            if (services.Any(service => typeof(DbConnection) == service.ServiceType))
            {
                services.Remove(services.SingleOrDefault(service => typeof(DbConnection) == service.ServiceType)!);
            }

            services.AddDbContext<EmailAuthEntityDbContext>(options =>
            {
                options.UseSqlServer(_dbContainer.GetConnectionString());
            });
        });
        
        builder.UseEnvironment("Development");
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        
        // Run Migrations on Database
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EmailAuthEntityDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public new Task DisposeAsync()
    {
        return _dbContainer.StopAsync();
    }
}