using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HomeBase.Database;

public class HomeBaseDbContextFactory : IDesignTimeDbContextFactory<HomeBaseDbContext>
{
    public HomeBaseDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("HOMEBASE_DB")
            ?? "Host=localhost;Port=5432;Database=homebase;Username=homebase;Password=homebase";

        var options = new DbContextOptionsBuilder<HomeBaseDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new HomeBaseDbContext(options);
    }
}
