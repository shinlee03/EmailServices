using System.Data.Common;
using System.Net.Mail;
using DotNet.Testcontainers.Builders;
using EmailService.Data;
using EmailService.Smtp;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
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

    public ISmtpClient MockSmtpClient;
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<EmailAuthEntityDbContext>));

            services.RemoveAll(typeof(DbConnection));
            services.AddDbContext<EmailAuthEntityDbContext>(options =>
            {
                options.UseSqlServer(_dbContainer.GetConnectionString());
            });
            
            // Add SmtpClient Mock to DI 
            services.RemoveAll(typeof(ISmtpClient));
            MockSmtpClient = Substitute.For<ISmtpClient>();
            MockSmtpClient.SendMailAsync(Arg.Any<MailMessage>()).ReturnsForAnyArgs(Task.CompletedTask);
            services.AddSingleton<ISmtpClient>(MockSmtpClient);
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