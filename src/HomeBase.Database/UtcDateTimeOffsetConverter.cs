using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HomeBase.Database;

public sealed class UtcDateTimeOffsetConverter()
    : ValueConverter<DateTimeOffset, DateTimeOffset>(
        value => value.ToUniversalTime(),
        value => value
    );
