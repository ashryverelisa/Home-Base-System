using HomeBase.Database.Enums;
using HomeBase.Features.Common;
using MudBlazor;

namespace HomeBase.Tests.Common;

public class ZonesTests
{
    [Theory]
    [InlineData(StorageZone.Fridge, "Kühlschrank", "Fridge")]
    [InlineData(StorageZone.Freezer, "Gefrierer", "Freezer")]
    [InlineData(StorageZone.Ambient, "Vorrat", "Pantry")]
    [InlineData(StorageZone.Room, "Raum", "Room")]
    public void Describe_IsLocalized(StorageZone zone, string german, string english)
    {
        using (new CultureScope("de-DE"))
        {
            Assert.Equal(german, Zones.Describe(zone));
        }

        using (new CultureScope("en-US"))
        {
            Assert.Equal(english, Zones.Describe(zone));
        }
    }

    [Fact]
    public void Icon_EveryZoneHasDistinctIcon()
    {
        var icons = Enum.GetValues<StorageZone>().Select(z => Zones.Icon(z)).ToList();

        Assert.Equal(icons.Count, icons.Distinct().Count());
        Assert.DoesNotContain(Icons.Material.Filled.HelpOutline, icons);
    }

    [Fact]
    public void Icon_NoZone_FallsBackToHelpIcon()
    {
        Assert.Equal(Icons.Material.Filled.HelpOutline, Zones.Icon(null));
    }

    [Theory]
    [InlineData(StorageZone.Fridge, 3, 3)]
    [InlineData(StorageZone.Ambient, 14, 14)]
    [InlineData(null, 3, 3)]
    [InlineData(StorageZone.Freezer, 3, Zones.FreezerWarningDays)]
    [InlineData(StorageZone.Freezer, 60, 60)]
    public void WarningDays_FreezerGetsAtLeastItsOwnLeadTime(StorageZone? zone, int days, int expected)
    {
        Assert.Equal(expected, Zones.WarningDays(zone, days));
    }

    [Theory]
    [InlineData(StorageZone.Fridge, StorageZone.Freezer, true)]
    [InlineData(StorageZone.Freezer, StorageZone.Fridge, true)]
    [InlineData(null, StorageZone.Freezer, true)]
    [InlineData(StorageZone.Fridge, StorageZone.Ambient, false)]
    [InlineData(StorageZone.Freezer, StorageZone.Freezer, false)]
    [InlineData(StorageZone.Fridge, null, false)]
    public void ChangesShelfLife_OnlyWhenFreezingOrThawing(StorageZone? from, StorageZone? to, bool expected)
    {
        Assert.Equal(expected, Zones.ChangesShelfLife(from, to));
    }
}
