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
}
