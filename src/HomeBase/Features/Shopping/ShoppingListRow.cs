using System.Linq.Expressions;
using HomeBase.Database.Entities;
using HomeBase.Database.Enums;

namespace HomeBase.Features.Shopping;

public sealed record ShoppingListRow(int Id, string Name, ShoppingListKind Kind, bool IsDefault)
{
    public static readonly Expression<Func<ShoppingList, ShoppingListRow>> Projection =
        list => new ShoppingListRow(list.Id, list.Name, list.Kind, list.IsDefault);

    public int OpenCount { get; init; }

    // Groceries come from the catalog and price history; everything else gets a price and priority by hand.
    public bool TracksPrices => Kind != ShoppingListKind.Groceries;
}
