using System.Net;
using System.Net.Mail;
using System.Threading.RateLimiting;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using EmailService.Authentication;
using EmailService.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddRateLimiter(opts =>
{
    opts.AddFixedWindowLimiter(policyName: "fixed", options =>
    {
        options.PermitLimit = int.Parse(Environment.GetEnvironmentVariable("RATE_PERLIMIT") ?? "10");
        options.Window = TimeSpan.FromHours(int.Parse(Environment.GetEnvironmentVariable("RATE_HOUR") ?? "1"));
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = int.Parse(Environment.GetEnvironmentVariable("RATE_QUEUELIMIT") ?? "10");
    });
});

string smtpAuthUsername = Environment.GetEnvironmentVariable("SMTP_USERNAME")!;
string smtpAuthPassword = Environment.GetEnvironmentVariable("SMTP_PASSWORD")!;
string smtpHostUrl = Environment.GetEnvironmentVariable("SMTP_GET")!;

var client = new SmtpClient(smtpHostUrl)
{
    Port = 587,
    Credentials = new NetworkCredential(smtpAuthUsername, smtpAuthPassword),
    EnableSsl = true
};

builder.Services.AddSingleton(client);

string connectionString;

 if (builder.Environment.IsDevelopment())
 {
     builder.Configuration.AddEnvironmentVariables().AddJsonFile("appsettings.Development.json");
     connectionString = builder.Configuration.GetConnectionString("AZURE_SQL_CONNECTIONSTRING")!;
 }
 else
 {
    connectionString = Environment.GetEnvironmentVariable("AZURE_SQL_CONNECTIONSTRING")!;
}

builder.Services.AddDbContext<EmailAuthEntityDbContext>(options => options.UseSqlServer(connectionString,
    optionsBuilder =>
    {
        optionsBuilder.EnableRetryOnFailure(3, TimeSpan.FromSeconds(10), null);
    }));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromMinutes(20);
    options.SlidingExpiration = true;
    options.AccessDeniedPath = "/";
    options.EventsType = typeof(CustomAuthentication);
});

builder.Services.AddScoped<CustomAuthentication>();

builder.Services.AddOpenTelemetry().UseAzureMonitor();

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "AllowSpecificOrigins",
        policy =>
        {
            policy.WithOrigins("http://shinlee.org", "http://www.shinlee.org")
                .AllowAnyMethod();
        });
});

var app = builder.Build();

app.UseRateLimiter();

app.UseSwagger(); 
app.UseSwaggerUI(options =>
{
    options.EnableTryItOutByDefault();
});

app.UseHttpsRedirection();
app.UseCookiePolicy(new CookiePolicyOptions
{
    MinimumSameSitePolicy = SameSiteMode.Strict
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.UseCors("AllowSpecificOrigins");

app.Run();