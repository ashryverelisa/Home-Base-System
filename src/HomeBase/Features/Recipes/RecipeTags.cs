namespace HomeBase.Features.Recipes;

public static class RecipeTags
{
    public static List<string> Parse(string? input) =>
        [
            .. (input ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.CurrentCultureIgnoreCase),
        ];

    public static string Format(IEnumerable<string> tags) => string.Join(", ", tags);
}
