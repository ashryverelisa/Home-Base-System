namespace HomeBase.Features.MasterData;

public static class ListOrder
{
    public static IReadOnlyList<int> Move(IReadOnlyList<int> orderedIds, int id, int offset)
    {
        var index = orderedIds.ToList().IndexOf(id);

        if (index < 0)
        {
            return orderedIds;
        }

        var target = Math.Clamp(index + offset, 0, orderedIds.Count - 1);
        var result = orderedIds.ToList();

        result.RemoveAt(index);
        result.Insert(target, id);

        return result;
    }
}
