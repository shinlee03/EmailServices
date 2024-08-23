using System.Net.Mail;
using System.Security.Claims;
using EmailService.Data.Repository;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EmailService.Controllers;

[ApiController]
[Route("api")]
public class EmailServiceController(
    ILogger<EmailServiceController> logger,
    IEmailAuthRepository emailAuthRepository,
    SmtpClient smtpClient)
    : ControllerBase
{
    private const string Sender = "DoNotReply@shinlee.org";
    
    /// <summary>
    /// Sends an authentication email to the supplied email. Can only send one every day to prevent Dos.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost]
    [Route("authenticate")]
    [EnableRateLimiting("fixed")]
    public async Task<IActionResult> Authenticate([FromForm] EmailAuthenticateRequest request)
    {
        bool created = false;
        try
        {
            var now = DateTime.UtcNow;
            var existingEntries = emailAuthRepository.GetEntities(emailFilter: (s) => (s == request.Email),
                verificationTimeFilter: (t) => (now - t).TotalDays <= 1);
            if (existingEntries.Any())
            {
                return BadRequest("Please reuse an already existing verification code.");
            }
            var authorizationCode = await CreateNewEntry(request.Email);
            created = true;
            var message = new MailMessage(Sender, request.Email, "Authentication Code for Shin Lee's portfolio",
                $"Your verification code is {authorizationCode}. Please note that you can reuse your verification code for the next 24 hours.");
            await smtpClient.SendMailAsync(message);
            return Created();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
            if (created)
            {
                await emailAuthRepository.RemoveEntity(emailFilter: (e) => e == request.Email);
            }
            return StatusCode(500, "Internal Server Error");
        }
    }
    
    /// <summary>
    /// Creates a session cookie and "logs" in.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost]
    [Route("session")]
    public async Task<IActionResult> CreateCookie([FromForm] CreateCookieRequest request)
    {
        if (!ValidToken(request.Email, request.AuthenticationCode))
        {
            return Unauthorized("Invalid token");
        }
        
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Email, request.Email),
            new Claim(ClaimTypes.Role, "Guest"),
            new Claim("AuthenticationCode", request.AuthenticationCode.ToString()),
        };
        
        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IssuedUtc = DateTime.UtcNow,
            IsPersistent = false,
            ExpiresUtc = DateTime.UtcNow.AddMinutes(20) // absolute expiry
        };
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);
        
        logger.LogInformation($"Guest {request.Email} created cookie at {DateTime.UtcNow} UTC.");
        return Created();
    }
    /// <summary>
    /// Clears the session cookie and "logs" out
    /// </summary>
    /// <returns></returns>
    [HttpDelete]
    [Route("session")]
    [Authorize]
    public async Task<IActionResult> DeleteCookie()
    {
        try
        {
            // var email = HttpContext.User.Claims.Single(x => x.Type == ClaimTypes.Email).Value;
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok();
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            return StatusCode(500, "Internal Server Error");
        }
    }
    /// <summary>
    /// Sends an email. Must be logged in. Recipient must be the email of the logged in user!
    /// </summary>
    /// <param name="request">EmailServiceRequest forms.</param>
    /// <returns> Status of email.</returns>
    [HttpPost]
    [Route("send")]
    [Authorize]
    [EnableRateLimiting("fixed")]
    public async Task<IActionResult> SendToMyself([FromForm] EmailServiceRequest request)
    {
        if (request.Recipient != HttpContext.User.Claims.Single(x => x.Type == ClaimTypes.Email).Value)
        {
            return Unauthorized("You can only send to your own email.");
        }
        var message = new MailMessage(Sender, request.Recipient, request.Subject, request.Body);
        try
        {
            await smtpClient.SendMailAsync(message);
            logger.LogInformation($"Email sent to {request.Recipient} with subject {request.Subject} and Content {request.Body}.");
            return Ok();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, $"Email sending failed with the following exception: {ex.Message}");
            return BadRequest();
        }
    }

    [HttpPost]
    [Route("sendtoshin")]
    [EnableCors("AllowSpecificOrigins")]
    public async Task<IActionResult> SendToShin([FromForm] SendToShinRequest request)
    {
        var message = new MailMessage(Sender, "shinlee@umich.edu", request.Subject, request.Body);
        try
        {
            await smtpClient.SendMailAsync(message);
            logger.LogInformation($"Email sent to shinlee@umich.edu with subject {request.Subject} and Content {request.Body}.");
            return Ok();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, $"Email sending failed with the following exception: {ex.Message}");
            return BadRequest();
        }
    }

    private async Task<Guid> CreateNewEntry(string email)
    {
        // remove already existing entries
        await emailAuthRepository.RemoveEntity(emailFilter: (e) => e == email);
        
        // create new one
        var createdEntity = await emailAuthRepository.AddEntity(email);
        return createdEntity.VerificationToken;
    }

    private bool ValidToken(string email, Guid token) =>
        emailAuthRepository.GetEntities(
            emailFilter: (e) => e == email,
            verificationTokenFilter: (t) => t == token,
            verificationTimeFilter: (d) => (DateTime.UtcNow - d).TotalDays <= 1,
            isValidFilter: (v) => v == true
        ).Any();
}