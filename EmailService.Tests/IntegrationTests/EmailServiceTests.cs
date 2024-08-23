using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;
using EmailService.Tests.Setup;
using FluentAssertions;
using FluentAssertions.Execution;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NSubstitute;

namespace EmailService.Tests.IntegrationTests;

public partial class EmailServiceTests : BaseIntegrationTest
{
    public EmailServiceTests(IntegrationTestWebFactory factory) : base(factory)
    {
        
    }

    [Fact]
    public async Task PostAuthenticate_ValidEmail_ReturnsCreated()
    {
        // Arrange
        await SetupData();
        var formData = new FormUrlEncodedContent([new("Email", "shinlee@umich.edu")]);
        var msgRegex = AuthEmailBodyRegex();
            
        // Act
        var response = await Client.PostAsync("/api/authenticate", formData);
        
        // Assert
        using var scope = new AssertionScope();
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        await Factory.MockSmtpClient.Received(1).SendMailAsync(Arg.Is<MailMessage>(msg =>
            msg.From!.Address == "DoNotReply@shinlee.org" &&
            msg.Subject == "Authentication Code for Shin Lee's portfolio" &&
            msgRegex.IsMatch(msg.Body) &&
            msg.To.Single().Address == "shinlee@umich.edu"
        ));
    }

    [Fact]
    public async Task PostAuthenticate_RequestTwice_ShouldFailSecond()
    {
        // Arrange
        await SetupData();
        var formData = new FormUrlEncodedContent([new("Email", "test@shinlee.org")]);
        var msgRegex = AuthEmailBodyRegex();
            
        // Act
        var response = await Client.PostAsync("/api/authenticate", formData);
        var secondResponse = await Client.PostAsync("/api/authenticate", formData);
        
        // Assert
        using var scope = new AssertionScope();
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await Factory.MockSmtpClient.Received(1).SendMailAsync(Arg.Is<MailMessage>(msg =>
            msg.From!.Address == "DoNotReply@shinlee.org" &&
            msg.Subject == "Authentication Code for Shin Lee's portfolio" &&
            msgRegex.IsMatch(msg.Body) &&
            msg.To.Single().Address == "test@shinlee.org"
        ));
        Factory.MockSmtpClient.ReceivedCalls().Should().HaveCount(1);
    }

    [Fact]
    public async Task PostSession_RequestWithInvalidEntry_ShouldFail()
    {
        // Arrange
        await SetupData();
        var formData = new FormUrlEncodedContent([new("Email", "doesnotexist@shinlee.org"), new("AuthenticationCode", MockEmailAuthEntities[0].VerificationToken.ToString())]);
        
        // Act
        var response = await Client.PostAsync("/api/session", formData);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var message = await response.Content.ReadAsStringAsync();
        message.Should().Be("Invalid token");
    }

    [Fact]
    public async Task PostSession_RequestWithMissingCode_ShouldFail()
    {
        // Arrange
        await SetupData();
        var formData = new FormUrlEncodedContent([new("Email", "first@first.com")]);
        
        // Act
        var response = await Client.PostAsync("/api/session", formData);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var message = await response.Content.ReadAsStringAsync();
        message.Should().Be("Authentication code is required.");
    }

    [Fact]
    public async Task PostSession_RequestWithMissingEmail_ShouldFailCaughtThroughDefaultValidator()
    {
        // Arrange
        await SetupData();
        var formData = new FormUrlEncodedContent([new("AuthenticationCode", MockEmailAuthEntities[0].VerificationToken.ToString())]);
        
        // Act
        var response = await Client.PostAsync("/api/session", formData);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = JsonConvert.DeserializeObject<Dictionary<string, object>>(await response.Content.ReadAsStringAsync());
        content.Should().ContainKey("errors");
        var errors = (content!["errors"] as JObject);
        errors!.TryGetValue("Email", out var e).Should().BeTrue();
        e!.GetType().Should().Be(typeof(JArray));
        ((e! as JArray)!).Single().Value<string>().Should().Be("The Email field is required.");
    }

    [GeneratedRegex(@"^Your verification code is [{]?[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}[}]?\. Please note that you can reuse your verification code for the next 24 hours\.$")]
    private static partial Regex AuthEmailBodyRegex();
}