using HomeBase.Database.Enums;
using HomeBase.Localization;
using MudBlazor;

namespace HomeBase.Features.Common;

public static class Zones
{
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
