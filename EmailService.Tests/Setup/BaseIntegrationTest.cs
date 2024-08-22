using System.Net.Mail;
using EmailService.Data;
using EmailService.Data.Repository;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ClearExtensions;

namespace EmailService.Tests.Setup;
[Collection("IntegrationTestCollection")]
public abstract class BaseIntegrationTest : IDisposable
{
    protected readonly SmtpClient SmtpClient;
    protected readonly IEmailAuthRepository EmailAuthRepository;
    protected readonly IServiceScope ServiceScope;
    protected readonly EmailAuthEntityDbContext DatabaseContext;

    IntegrationTestWebFactory Factory;
    public BaseIntegrationTest(IntegrationTestWebFactory factory)
    {
        Factory = factory;
        ServiceScope = Factory.Services.CreateScope();
        DatabaseContext = ServiceScope.ServiceProvider.GetRequiredService<EmailAuthEntityDbContext>();
        EmailAuthRepository = ServiceScope.ServiceProvider.GetRequiredService<IEmailAuthRepository>();
        
        // mock external clients
        SmtpClient = Substitute.For<SmtpClient>();
    }

    public void Dispose()
    {
        // Clear Substitutes
        SmtpClient.ClearSubstitute();
        
        // Clear Database
        ClearData();
    }

    protected readonly EmailAuthEntity[] MockEmailAuthEntities =
    {
        new EmailAuthEntity
        {
            Email = "first@first.com",
            IsInvalid = false,
            VerificationTime = new DateTime(2024, 05, 02, 12, 0, 0),
            VerificationToken = Guid.NewGuid()
        },
        new EmailAuthEntity
        {
            Email = "second@second.com",
            IsInvalid = true,
            VerificationToken = Guid.NewGuid(),
            VerificationTime = DateTime.UtcNow.Subtract(TimeSpan.FromDays(1))
        },
        new EmailAuthEntity
        {
            Email = "second@second.com",
            IsInvalid = false,
            VerificationToken = Guid.NewGuid(),
            VerificationTime = DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(5))
        },
        new EmailAuthEntity
        {
            Email = "third@third.com",
            IsInvalid = false,
            VerificationTime = DateTime.UtcNow,
            VerificationToken = Guid.NewGuid()
        },
    };
    protected async Task SetupData()
    {
        // clear database
        ClearData();
        
        // Insert Mock Data
        await DatabaseContext.EmailAuthEntities.AddRangeAsync(MockEmailAuthEntities);
        await DatabaseContext.SaveChangesAsync();
    }
    
    private void ClearData() => DatabaseContext.EmailAuthEntities.RemoveRange(DatabaseContext.EmailAuthEntities);
}