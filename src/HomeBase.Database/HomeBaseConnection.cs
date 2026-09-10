namespace HomeBase.Database;

public static class HomeBaseConnection
{
    public const string EnvironmentVariable = "HOMEBASE_DB";
    public const string ConfigurationKey = "HomeBase";

    public const string LocalDevelopmentDefault =
        "Host=localhost;Port=5432;Database=homebase;Username=homebase;Password=homebase";

    public static string Resolve(string? fromConfiguration = null) =>
        Environment.GetEnvironmentVariable(EnvironmentVariable) is { Length: > 0 } fromEnvironment
            ? fromEnvironment
            : fromConfiguration is { Length: > 0 } ? fromConfiguration : LocalDevelopmentDefault;
}
