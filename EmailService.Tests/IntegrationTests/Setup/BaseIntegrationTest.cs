using System.Net.Mail;
using EmailService.Data;
using EmailService.Data.Repository;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ClearExtensions;

namespace EmailService.Tests.IntegrationTests.Setup;

public class BaseIntegrationTest : IDisposable, IClassFixture<IntegrationTestWebFactory>
{
    protected readonly SmtpClient SmtpClient;
    protected readonly IEmailAuthRepository EmailAuthRepository;
    protected readonly IServiceScope ServiceScope;
    protected readonly EmailAuthEntityDbContext DatabaseContext;

    protected BaseIntegrationTest(IntegrationTestWebFactory factory)
    {
        ServiceScope = factory.Services.CreateScope();
        DatabaseContext = ServiceScope.ServiceProvider.GetRequiredService<EmailAuthEntityDbContext>();
        EmailAuthRepository = ServiceScope.ServiceProvider.GetRequiredService<IEmailAuthRepository>();
        
        // mock external clients
        SmtpClient = Substitute.For<SmtpClient>();
    }

    public void Dispose()
    {
        // Dispose scope
        ServiceScope.Dispose();
        
        // Clear Substitutes
        SmtpClient.ClearSubstitute();
    }
    
}