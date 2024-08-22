using EmailService.Tests.Setup;

namespace EmailService.Tests.IntegrationTests;

public class EmailServiceTests : BaseIntegrationTest
{
    public EmailServiceTests(IntegrationTestWebFactory factory) : base(factory)
    {
        
    }

    [Fact]
    public async Task PostAuthenticate_ValidEmail_ReturnsSuccess()
    {
        // Arrange
        
        
    }
}