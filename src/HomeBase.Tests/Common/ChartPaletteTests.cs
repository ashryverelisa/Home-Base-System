using HomeBase.Features.Common;

namespace HomeBase.Tests.Common;

public class ChartPaletteTests
{
    [Fact]
    public void For_LightAndDark_HaveSameSlotCountButDifferentColors()
    {
        var light = ChartPalette.For(darkMode: false);
        var dark = ChartPalette.For(darkMode: true);

        Assert.Equal(light.Length, dark.Length);
        Assert.All(light, color => Assert.DoesNotContain(color, dark));
    }

    [Fact]
    public void Slot_ReturnsSingleColorOfThatSlot()
    {
        Assert.Equal([ChartPalette.For(true)[2]], ChartPalette.Slot(true, 2));
    }
}
