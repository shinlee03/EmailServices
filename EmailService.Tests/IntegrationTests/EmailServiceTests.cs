using System.Net;
using System.Net.Mail;
using EmailService.Tests.Setup;
using FluentAssertions;
using FluentAssertions.Execution;
using NSubstitute;

namespace EmailService.Tests.IntegrationTests;

public class EmailServiceTests : BaseIntegrationTest
{
    private readonly IntegrationTestWebFactory Factory;
    public EmailServiceTests(IntegrationTestWebFactory factory) : base(factory)
    {
        Factory = factory;
    }

    [Fact]
    public async Task PostAuthenticate_ValidEmail_ReturnsSuccess()
    {
        // Arrange
        var formData = new FormUrlEncodedContent([new KeyValuePair<string, string>("Email", "shinlee@umich.edu")]);
        
        // Act
        var response = await HttpClient.PostAsync("/api/authenticate", formData);
        
        // Assert
        using var scope = new AssertionScope();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Factory.SmtpClient.ReceivedWithAnyArgs(1).Send(Arg.Any<MailMessage>());
    }

    [Fact]
    public async Task PostAuthenticate_RequestTwice_ShouldFailSecond()
    {
        
    }
}