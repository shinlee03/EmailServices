using EmailService.Data;
using EmailService.Tests.Setup;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Extensions;

namespace EmailService.Tests.IntegrationTests;

public class EmailAuthRepositoryTests : BaseIntegrationTest
{
    public EmailAuthRepositoryTests(IntegrationTestWebFactory factory) : base(factory)
    {
        
    }
    
    private readonly TimeSpan _maxDelay = TimeSpan.FromSeconds(10);
    
    [Fact]
    public async Task AddEntity_ValidEntries_Success()
    {
        // Arrange
        var email = "integrationtest@shinlee.org";
        
        // Act
        var entity = await EmailAuthRepository.AddEntity(email);
        
        // Assert
        var scope = new AssertionScope();
        
        entity.Email.Should().Be(email);
        entity.VerificationTime.Should().BeWithin(_maxDelay).Before(DateTime.Now);
        entity.VerificationToken.Should().NotBeEmpty();
        entity.IsInvalid.Should().BeFalse();

        DatabaseContext.EmailAuthEntities.AsQueryable()
            .Where(e => e.Email == entity.Email)
            .Where(e => e.VerificationTime == entity.VerificationTime)
            .Where(e => e.VerificationToken == entity.VerificationToken)
            .Where(e => e.IsInvalid == entity.IsInvalid)
            .Should().HaveCount(1);
    }
}