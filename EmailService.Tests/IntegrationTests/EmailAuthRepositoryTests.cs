using EmailService.Data;
using EmailService.Tests.Setup;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.EntityFrameworkCore;

namespace EmailService.Tests.IntegrationTests;

public class EmailAuthRepositoryTests(IntegrationTestWebFactory factory) : BaseIntegrationTest(factory)
{
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
        entity.VerificationTime.Should().BeWithin(_maxDelay).Before(DateTime.UtcNow);
        entity.VerificationToken.Should().NotBeEmpty();
        entity.IsInvalid.Should().BeFalse();

        DatabaseContext.EmailAuthEntities.AsQueryable()
            .Where(e => e.Email == entity.Email)
            .Where(e => e.VerificationTime == entity.VerificationTime)
            .Where(e => e.VerificationToken == entity.VerificationToken)
            .Where(e => e.IsInvalid == entity.IsInvalid)
            .Should().HaveCount(1);
    }

    [Fact]
    public async Task RemoveEntity_ValidFilter_Success()
    {
        // Arrange
        await SetupData();
        
        // Act
        var numRemovals = await EmailAuthRepository.RemoveEntity(
            emailFilter: (e) => e == "second@second.com");
        
        // Assert
        using var scope = new AssertionScope();
        numRemovals.Should().Be(2);

        var query = DatabaseContext.EmailAuthEntities.ToArray();
        query.Should().ContainInOrder([MockEmailAuthEntities[0], MockEmailAuthEntities[3]]);
    }

    [Fact]
    public async Task RemoveEntity_InvalidFilter_ShouldReturnZero()
    {
        // Arrange
        await SetupData();
        
        // Act
        var numRemovals = await EmailAuthRepository.RemoveEntity(emailFilter: (e) => e == "doesnotexist@hello.hello");
        
        // Assert
        using var scope = new AssertionScope();
        numRemovals.Should().Be(0);
        
        var query = DatabaseContext.EmailAuthEntities.ToArray();
        query.Should().ContainInOrder(MockEmailAuthEntities);
    }

    [Fact]
    public async Task GetEntities_ValidFilter_ShouldReturnTwo()
    {
        // Arrange
        await SetupData();
        
        // Act
        var entities = EmailAuthRepository
            .GetEntities(emailFilter: (e) => e == "second@second.com")
            .ToArray();
        
        // Assert
        using var scope = new AssertionScope();
        entities.Should().HaveCount(2);
        entities.Should().ContainInOrder([MockEmailAuthEntities[1], MockEmailAuthEntities[2]]);
    }

    [Fact]
    public async Task GetEntities_UniqueFilter_ShouldReturnOne()
    {
        // Arrange
        await SetupData();
        
        // Act
        var entities = EmailAuthRepository
            .GetEntities(verificationTokenFilter: (token) => token == MockEmailAuthEntities[1].VerificationToken)
            .ToArray();
        
        // Assert
        using var scope = new AssertionScope();
        entities.Should().HaveCount(1);
        entities.Should().ContainSingle(e => e == MockEmailAuthEntities[1]);
    }

    [Fact]
    public async Task UpdateEntity_ParamsNullNotNullMix_Success()
    {
        // Arrange
        await SetupData();
        
        // Act
        var updatedEntity = await EmailAuthRepository.UpdateEntity(
                MockEmailAuthEntities[2],
                email: "five@five.com",
                verificationTime: DateTime.UtcNow);
        
        // Assert
        using var scope = new AssertionScope();
        MockEmailAuthEntities[2].Should().Be(updatedEntity); // should return the same tracked entity.
        DatabaseContext.Entry(MockEmailAuthEntities[2]).State.Should().NotBe(EntityState.Detached); // should be in sync.
        
        updatedEntity.Email.Should().Be("five@five.com");
        updatedEntity.VerificationToken.Should().Be(MockEmailAuthEntities[2].VerificationToken);
        updatedEntity.IsInvalid.Should().BeFalse();
        updatedEntity.VerificationTime.Should().BeWithin(_maxDelay).Before(DateTime.UtcNow);
    }
}