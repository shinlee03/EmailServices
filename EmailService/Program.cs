using EmailService;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddServices();

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

public partial class Program
{
    
}