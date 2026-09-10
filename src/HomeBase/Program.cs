using System.Globalization;
using HomeBase.Components;
using HomeBase.Data;
using HomeBase.Database;
using HomeBase.Features.Catalog;
using HomeBase.Features.Inventory;
using HomeBase.Localization;
using Microsoft.AspNetCore.Localization;
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

builder.Services.AddScoped<CatalogService>();
builder.Services.AddScoped<InventoryService>();

builder.Services.AddHostedService<DatabaseInitializer>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseRequestLocalization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapCultureEndpoints();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
