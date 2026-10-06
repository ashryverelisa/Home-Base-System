using HomeBase.Database;
using HomeBase.Features;
using HomeBase.Features.MealPlan;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;

namespace HomeBase.Tests;

public class FeatureRegistrationTests
{
    // Catches a constructor dependency that Program.cs never registers.
    [Fact]
    public void AddHomeBaseFeatures_EveryServiceResolves()
    {
        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build())
            .AddSingleton(Substitute.For<IHostEnvironment>())
            .AddLogging()
            .AddLocalization()
            .AddDbContextFactory<HomeBaseDbContext>(options => options.UseNpgsql("Host=unused"))
            .AddHomeBaseFeatures();

        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true }
        );
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IMealPlanService>());
    }
}
