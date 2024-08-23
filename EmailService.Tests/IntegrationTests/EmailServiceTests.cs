using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;
using EmailService.Tests.Setup;
using FluentAssertions;
using FluentAssertions.Execution;
using NSubstitute;

namespace EmailService.Tests.IntegrationTests;

public partial class EmailServiceTests : BaseIntegrationTest
{
    public EmailServiceTests(IntegrationTestWebFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task PostAuthenticate_ValidEmail_ReturnsSuccess()
    {
        // Arrange
        var formData = new FormUrlEncodedContent([new KeyValuePair<string, string>("Email", "shinlee@umich.edu")]);
        var msgRegex = AuthEmailBodyRegex();
            
        // Act
        var response = await HttpClient.PostAsync("/api/authenticate", formData);
        
        // Assert
        using var scope = new AssertionScope();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await SmtpClient.Received(1).SendMailAsync(Arg.Is<MailMessage>(msg =>
            msg.From!.Address == "DoNotReply@shinlee.org" &&
            msg.Subject == "Authentication Code for Shin Lee's portfolio" &&
            msgRegex.IsMatch(msg.Body) &&
            msg.To.First().Address == "shinlee@umich.edu"
        ));
    }

    [Fact]
    public async Task PostAuthenticate_RequestTwice_ShouldFailSecond()
    {
        
    }

    [GeneratedRegex(@"^Your verification code is [{]?[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}[}]?\. Please note that you can reuse your verification code for the next 24 hours\.$")]
    private static partial Regex AuthEmailBodyRegex();
}