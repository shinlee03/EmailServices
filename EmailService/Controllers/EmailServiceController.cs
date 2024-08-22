using System.Globalization;
using System.Net.Mail;
using System.Security.Claims;
using EmailService.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EmailService.Controllers;

[ApiController]
[Route("api")]
public class EmailServiceController : ControllerBase
{
    private readonly SmtpClient client;
    private readonly ILogger<EmailServiceController> logger;
    private readonly EmailAuthEntityDbContext context;
    
    public EmailServiceController(SmtpClient _client, ILogger<EmailServiceController> _logger, EmailAuthEntityDbContext _context)
    {
        client = _client;
        logger = _logger;
        context = _context;
    }

    private const string sender = "DoNotReply@shinlee.org";
    
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
            if (context.EmailAuthEntities.AsEnumerable().Any(x => x.Email == request.Email && (now - x.VerificationTime).TotalDays <= 1))
            {
                return BadRequest("Please reuse an already existing verification code.");
            }
            var authorizationCode = await CreateNewEntry(request.Email);
            created = true;
            var message = new MailMessage(sender, request.Email, "Authentication Code for Shin Lee's portfolio",
                $"Your verification code is {authorizationCode}. Please note that you can reuse your verification code for the next 24 hours.");
            client.Send(message);
            return Created();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
            if (created)
            {
                context.EmailAuthEntities.Remove(context.EmailAuthEntities.First(x => x.Email == request.Email));
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
    public IActionResult SendToMyself([FromForm] EmailServiceRequest request)
    {
        if (request.Recipient != HttpContext.User.Claims.Single(x => x.Type == ClaimTypes.Email).Value)
        {
            return Unauthorized("You can only send to your own email.");
        }
        var message = new MailMessage(sender, request.Recipient, request.Subject, request.Body);
        try
        {
            client.Send(message);
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
    public IActionResult SendToShin([FromForm] SendToShinRequest request)
    {
        var message = new MailMessage(sender, "shinlee@umich.edu", request.Subject, request.Body);
        try
        {
            client.Send(message);
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
        context.EmailAuthEntities.RemoveRange(context.EmailAuthEntities.Where(x => x.Email == email).ToList());
        
        // create new one
        var authorizationCode = Guid.NewGuid();
        var emailAuthenticationEntity = new EmailAuthEntity
        {
            Email = email,
            VerificationToken = authorizationCode,
            VerificationTime = DateTime.UtcNow,
            IsInvalid = false
        };
        await context.EmailAuthEntities.AddAsync(emailAuthenticationEntity);
        await context.SaveChangesAsync();
        return authorizationCode;
    }

    private bool ValidToken(string email, Guid token) => context.EmailAuthEntities.AsEnumerable().Any(x =>
       !x.IsInvalid && x.Email == email && x.VerificationToken == token && (DateTime.UtcNow - x.VerificationTime).TotalDays <= 1.0);
}