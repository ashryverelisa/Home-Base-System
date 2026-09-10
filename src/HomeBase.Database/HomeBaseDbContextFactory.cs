using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HomeBase.Database;

public class HomeBaseDbContextFactory : IDesignTimeDbContextFactory<HomeBaseDbContext>
{
    public HomeBaseDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<HomeBaseDbContext>()
            .UseNpgsql(HomeBaseConnection.Resolve())
            .Options;

        return new HomeBaseDbContext(options);
    }
}
