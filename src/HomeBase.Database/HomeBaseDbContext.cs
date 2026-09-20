using HomeBase.Database.Entities;
using HomeBase.Database.Views;
using Microsoft.EntityFrameworkCore;

namespace HomeBase.Database;

public class HomeBaseDbContext(DbContextOptions<HomeBaseDbContext> options) : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<StorageLocation> StorageLocations => Set<StorageLocation>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductAlias> ProductAliases => Set<ProductAlias>();
    public DbSet<StockLot> StockLots => Set<StockLot>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<AssetDocument> AssetDocuments => Set<AssetDocument>();
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<PurchaseItem> PurchaseItems => Set<PurchaseItem>();
    public DbSet<ShoppingList> ShoppingLists => Set<ShoppingList>();
    public DbSet<ShoppingListItem> ShoppingListItems => Set<ShoppingListItem>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();
    public DbSet<MealPlanEntry> MealPlanEntries => Set<MealPlanEntry>();

    public DbSet<EffectiveLine> EffectiveLines => Set<EffectiveLine>();
    public DbSet<MonthlySpend> MonthlySpend => Set<MonthlySpend>();
    public DbSet<PricePoint> PriceHistory => Set<PricePoint>();
    public DbSet<DepositBalance> DepositBalance => Set<DepositBalance>();
    public DbSet<BestStorePrice> BestStorePrices => Set<BestStorePrice>();
    public DbSet<ConsumptionRate> ConsumptionRates => Set<ConsumptionRate>();
    public DbSet<ReachDays> ReachDays => Set<ReachDays>();
    public DbSet<WasteCost> WasteCosts => Set<WasteCost>();
    public DbSet<BasketIndexPoint> BasketIndex => Set<BasketIndexPoint>();
    public DbSet<PromoSaving> PromoSavings => Set<PromoSaving>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
        optionsBuilder.UseSnakeCaseNamingConvention();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<UtcDateTimeOffsetConverter>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pg_trgm");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HomeBaseDbContext).Assembly);
    }
}
