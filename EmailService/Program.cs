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

if (app.Environment.IsDevelopment())
{
    app.Use(async (ctx, next) =>
    {
        try
        {
            await next.Invoke();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    });
}

app.UseAuthentication();
app.UseAuthorization();
app.UseRouting();
app.MapControllers();

app.UseCors("AllowSpecificOrigins");

app.Run();

public partial class Program
{
    
}