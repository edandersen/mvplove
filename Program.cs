using mvcmcpmvp.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<MvpDataService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Block requests that didn't come through the Cloudflare CDN.
// Set CloudflareOriginSecret in Azure App Service → Configuration → App Settings.
// Leave empty in development to skip the check.
var cfSecret = app.Configuration["CloudflareOriginSecret"];
if (!string.IsNullOrWhiteSpace(cfSecret))
{
    app.Use(async (context, next) =>
    {
        if (!context.Request.Headers.TryGetValue("Secret", out var value) || value != cfSecret)
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsync("Forbidden");
            return;
        }
        await next(context);
    });
}

app.UseRouting();
app.UseAuthorization();
app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Mvp}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
