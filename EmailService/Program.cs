using System.Net;
using System.Net.Mail;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

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

builder.Services.AddSingleton<SmtpClient>(client);

var app = builder.Build();

app.UseRateLimiter();

app.UseSwagger(); 
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();