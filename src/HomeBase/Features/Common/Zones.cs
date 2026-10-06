using HomeBase.Database.Enums;
using HomeBase.Localization;
using MudBlazor;

namespace HomeBase.Features.Common;

public static class Zones
{
    public const int FreezerWarningDays = 30;

    // Frozen goods keep for months; a fridge-sized lead time would come too late to plan thawing.
    public static int WarningDays(StorageZone? zone, int days) =>
        zone == StorageZone.Freezer ? Math.Max(days, FreezerWarningDays) : days;

    // Freezing or thawing changes how long something keeps; the printed date no longer applies.
    public static bool ChangesShelfLife(StorageZone? from, StorageZone? to) =>
        (from == StorageZone.Freezer) != (to == StorageZone.Freezer);

    public static string Describe(StorageZone zone) =>
        AppStrings.Get(
            zone switch
            {
                StorageZone.Fridge => "Zone.Fridge",
                StorageZone.Freezer => "Zone.Freezer",
                StorageZone.Ambient => "Zone.Ambient",
                _ => "Zone.Room",
            }
        );

    public static string Icon(StorageZone? zone) =>
        zone switch
        {
            StorageZone.Fridge => Icons.Material.Filled.Kitchen,
            StorageZone.Freezer => Icons.Material.Filled.AcUnit,
            StorageZone.Ambient => Icons.Material.Filled.Inventory2,
            StorageZone.Room => Icons.Material.Filled.DoorFront,
            _ => Icons.Material.Filled.HelpOutline,
        };
}
