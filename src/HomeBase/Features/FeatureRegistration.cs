using HomeBase.Features.Analytics;
using HomeBase.Features.Assets;
using HomeBase.Features.Catalog;
using HomeBase.Features.Ingest;
using HomeBase.Features.Inventory;
using HomeBase.Features.MasterData;
using HomeBase.Features.MealPlan;
using HomeBase.Features.Purchases;
using HomeBase.Features.Recipes;
using HomeBase.Features.Shopping;

namespace HomeBase.Features;

public static class FeatureRegistration
{
    public static IServiceCollection AddHomeBaseFeatures(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IAssetDocumentStore, AssetDocumentStore>();

        services.AddScoped<IAssetService, AssetService>();
        services.AddScoped<ICatalogService, CatalogService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IShoppingService, ShoppingService>();
        services.AddScoped<IPurchaseService, PurchaseService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<IReceiptIngestService, ReceiptIngestService>();
        services.AddScoped<IRecipeService, RecipeService>();
        services.AddScoped<IRecipeIngestService, RecipeIngestService>();
        services.AddScoped<IMealPlanService, MealPlanService>();
        services.AddScoped<IMasterDataService, MasterDataService>();

        services.AddHttpClient<IOpenFoodFacts, OpenFoodFactsClient>(client =>
        {
            client.BaseAddress = new Uri("https://world.openfoodfacts.org/");
            client.Timeout = TimeSpan.FromSeconds(5);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "HomeBase/1.0 (+https://github.com/ashryverelisa/Home-Base-System)"
            );
        });

        services.AddHostedService<AutoRestockService>();

        return services;
    }
}
