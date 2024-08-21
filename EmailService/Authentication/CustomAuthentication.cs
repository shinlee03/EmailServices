using System.Security.Claims;
using EmailService.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace EmailService.Authentication;

public class CustomAuthentication : CookieAuthenticationEvents
{
    private readonly EmailAuthEntityDbContext auth;

    public CustomAuthentication(EmailAuthEntityDbContext _context)
    {
        auth = _context;
    }

    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var principal = context.Principal;
        var email = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email).Value;
        var authCode = principal.Claims.FirstOrDefault(c => c.Type == "AuthenticationCode").Value;
        if (!IsValid(email,authCode))
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }

    private bool IsValid(string email, string authenticationCode)
    {
        return auth.EmailAuthEntities.Any(x =>
            x.Email == email && !x.IsInvalid && x.VerificationToken == Guid.Parse(authenticationCode));
    }
}