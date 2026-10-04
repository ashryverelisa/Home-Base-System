using HomeBase.Features.Recipes;

namespace HomeBase.Tests.Recipes;

public class RecipeTagsTests
{
    [Fact]
    public void Parse_SplitsTrimsAndDropsEmptyEntries()
    {
        Assert.Equal(
            ["vegetarisch", "schnell", "Pasta"],
            RecipeTags.Parse(" vegetarisch ,schnell,, Pasta ,")
        );
    }

    [Fact]
    public void Parse_RemovesDuplicatesIgnoringCaseKeepingFirstSpelling()
    {
        Assert.Equal(["Vegan", "Suppe"], RecipeTags.Parse("Vegan, Suppe, vegan, SUPPE, VEGAN"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" , ,")]
    public void Parse_NoTags_ReturnsEmpty(string? input)
    {
        Assert.Empty(RecipeTags.Parse(input));
    }

    [Fact]
    public void Format_AndParse_RoundTrip()
    {
        List<string> tags = ["Frühstück", "süß", "Ofen"];

        Assert.Equal("Frühstück, süß, Ofen", RecipeTags.Format(tags));
        Assert.Equal(tags, RecipeTags.Parse(RecipeTags.Format(tags)));
    }
}
