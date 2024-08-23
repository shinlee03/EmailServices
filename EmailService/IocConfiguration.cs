using System.Net;
using System.Net.Mail;
using System.Threading.RateLimiting;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using EmailService.Authentication;
using EmailService.Data;
using EmailService.Data.Repository;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace EmailService;

public static class IocConfiguration
{
    public static IServiceCollection AddServices(this WebApplicationBuilder builder)
    {
        var services = builder.Services;
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        services.AddRateLimiter(opts =>
        {
            opts.AddFixedWindowLimiter("fixed", options =>
            {
                options.PermitLimit = int.Parse(Environment.GetEnvironmentVariable("RATE_PERLIMIT") ?? "10");
                options.Window = TimeSpan.FromHours(int.Parse(Environment.GetEnvironmentVariable("RATE_HOUR") ?? "1"));
                options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                options.QueueLimit = int.Parse(Environment.GetEnvironmentVariable("RATE_QUEUELIMIT") ?? "10");
            });
        });

        var smtpAuthUsername = Environment.GetEnvironmentVariable("SMTP_USERNAME")!;
        var smtpAuthPassword = Environment.GetEnvironmentVariable("SMTP_PASSWORD")!;
        var smtpHostUrl = Environment.GetEnvironmentVariable("SMTP_GET")!;

        var client = new SmtpClient(smtpHostUrl)
        {
            Port = 587,
            Credentials = new NetworkCredential(smtpAuthUsername, smtpAuthPassword),
            EnableSsl = true
        };

        services.AddSingleton<SmtpClient>(client);

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

        services.AddDbContext<EmailAuthEntityDbContext>(options => options.UseSqlServer(connectionString,
            optionsBuilder => { optionsBuilder.EnableRetryOnFailure(3, TimeSpan.FromSeconds(10), null); }));

        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
        {
            options.ExpireTimeSpan = TimeSpan.FromMinutes(20);
            options.SlidingExpiration = true;
            options.AccessDeniedPath = "/";
            options.EventsType = typeof(CustomAuthentication);
        });

        services.AddScoped<CustomAuthentication>();
        if (builder.Environment.IsProduction())
        {
            services.AddOpenTelemetry().UseAzureMonitor();
        }
        services.AddCors(options =>
        {
            options.AddPolicy("AllowSpecificOrigins",
                policy =>
                {
                    policy.WithOrigins("http://shinlee.org", "http://www.shinlee.org")
                        .AllowAnyMethod();
                });
        });

        services.AddScoped<IEmailAuthRepository, EmailAuthRepository>();
        return services;
    }
}