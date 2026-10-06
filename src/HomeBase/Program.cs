using System.Globalization;
using HomeBase.Components;
using HomeBase.Data;
using HomeBase.Database;
using HomeBase.Features;
using HomeBase.Features.Assets;
using HomeBase.Features.Ingest;
using HomeBase.Localization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;

CultureInfo.DefaultThreadCurrentCulture = SupportedCultures.Default;
CultureInfo.DefaultThreadCurrentUICulture = SupportedCultures.Default;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.Services.AddLocalization();

builder.Services.AddMudServices();

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.SetDefaultCulture(SupportedCultures.DefaultName);
    options.AddSupportedCultures([.. SupportedCultures.Names]);
    options.AddSupportedUICultures([.. SupportedCultures.Names]);
    options.ApplyCurrentCultureToResponseHeaders = true;
    options.RequestCultureProviders = [new CookieRequestCultureProvider()];
});

var connectionString = HomeBaseConnection.Resolve(
    builder.Configuration.GetConnectionString(HomeBaseConnection.ConfigurationKey)
);

builder.Services.AddDbContextFactory<HomeBaseDbContext>(options =>
    options.UseNpgsql(connectionString)
);

// Hosted services start in registration order: migrate before the features' background jobs run.
builder.Services.AddHostedService<DatabaseInitializer>();
builder.Services.AddHomeBaseFeatures();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter(
        ApiKeyFilter.RateLimitPolicy,
        limiter =>
        {
            limiter.PermitLimit = 60;
            limiter.Window = TimeSpan.FromMinutes(1);
            limiter.QueueLimit = 0;
        }
    );
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseRequestLocalization();

app.UseRateLimiter();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapCultureEndpoints();
app.MapIngestEndpoints();
app.MapAssetDocumentEndpoints();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
