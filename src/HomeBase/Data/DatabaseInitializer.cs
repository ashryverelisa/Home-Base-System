using HomeBase.Database;
using HomeBase.Database.Entities;
using HomeBase.Database.Enums;
using Microsoft.EntityFrameworkCore;

namespace HomeBase.Data;

public sealed partial class DatabaseInitializer(
    IDbContextFactory<HomeBaseDbContext> factory,
    ILogger<DatabaseInitializer> logger
) : IHostedService
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Seeded master data")]
    private static partial void LogSeeded(ILogger logger);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        await db.Database.MigrateAsync(cancellationToken);

        var seeded = await SeedStorageLocationsAsync(db, cancellationToken);
        seeded |= await SeedCategoriesAsync(db, cancellationToken);
        seeded |= await SeedShoppingListsAsync(db, cancellationToken);

        if (seeded)
        {
            await db.SaveChangesAsync(cancellationToken);
            LogSeeded(logger);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task<bool> SeedStorageLocationsAsync(
        HomeBaseDbContext db,
        CancellationToken ct
    )
    {
        if (await db.StorageLocations.AnyAsync(ct))
        {
            return false;
        }

        db.StorageLocations.AddRange(
            new StorageLocation
            {
                Name = "Küche",
                Zone = StorageZone.Ambient,
                Children =
                [
                    new StorageLocation { Name = "Kühlschrank", Zone = StorageZone.Fridge },
                    new StorageLocation { Name = "Gefriertruhe", Zone = StorageZone.Freezer },
                    new StorageLocation { Name = "Vorratsregal", Zone = StorageZone.Ambient },
                ],
            },
            new StorageLocation
            {
                Name = "Keller",
                Zone = StorageZone.Ambient,
                Children =
                [
                    new StorageLocation { Name = "Getränkelager", Zone = StorageZone.Ambient },
                ],
            },
            new StorageLocation { Name = "Arbeitszimmer", Zone = StorageZone.Room },
            new StorageLocation { Name = "Bad", Zone = StorageZone.Room }
        );

        return true;
    }

    private static async Task<bool> SeedCategoriesAsync(HomeBaseDbContext db, CancellationToken ct)
    {
        if (await db.Categories.AnyAsync(ct))
        {
            return false;
        }

        (string Name, CategoryKind Kind)[] categories =
        [
            ("Milchprodukte", CategoryKind.Food),
            ("Backwaren", CategoryKind.Food),
            ("Obst & Gemüse", CategoryKind.Food),
            ("Fleisch & Fisch", CategoryKind.Food),
            ("Trockenwaren", CategoryKind.Food),
            ("Konserven", CategoryKind.Food),
            ("Tiefkühl", CategoryKind.Food),
            ("Getränke", CategoryKind.Food),
            ("Süßwaren", CategoryKind.Food),
            ("Gewürze & Saucen", CategoryKind.Food),
            ("Computer & Zubehör", CategoryKind.Tech),
            ("Audio & Video", CategoryKind.Tech),
            ("Haushaltsgeräte", CategoryKind.Tech),
            ("Netzwerk", CategoryKind.Tech),
            ("Reinigung", CategoryKind.Household),
            ("Hygiene", CategoryKind.Household),
            ("Papierwaren", CategoryKind.Household),
            ("Möbel", CategoryKind.Household),
            ("Sonstiges", CategoryKind.Other),
        ];

        db.Categories.AddRange(
            categories.Select(c => new Category { Name = c.Name, Kind = c.Kind })
        );

        return true;
    }

    private static async Task<bool> SeedShoppingListsAsync(
        HomeBaseDbContext db,
        CancellationToken ct
    )
    {
        if (await db.ShoppingLists.AnyAsync(ct))
        {
            return false;
        }

        db.ShoppingLists.AddRange(
            new ShoppingList
            {
                Name = "Lebensmittel",
                Kind = ShoppingListKind.Groceries,
                IsDefault = true,
                SortOrder = 0,
            },
            new ShoppingList
            {
                Name = "Technik",
                Kind = ShoppingListKind.Tech,
                SortOrder = 1,
            },
            new ShoppingList
            {
                Name = "Haushalt",
                Kind = ShoppingListKind.Household,
                SortOrder = 2,
            },
            new ShoppingList
            {
                Name = "Sonstiges",
                Kind = ShoppingListKind.Other,
                SortOrder = 3,
            }
        );

        return true;
    }
}
