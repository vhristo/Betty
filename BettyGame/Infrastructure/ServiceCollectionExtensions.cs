using BettyGame.Config;
using BettyGame.Game;
using BettyGame.Services;
using BettyGame.Wallet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BettyGame.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static void ConfigureServices(IServiceCollection serviceCollection, IConfiguration configuration)
    {
        serviceCollection.AddLogging();
        serviceCollection.AddSingleton(configuration.GetSection("GameSettings").Get<GameSettings>());
        serviceCollection.AddSingleton<ILogger>(sp => sp.GetRequiredService<ILogger<Program>>());
        serviceCollection.AddSingleton<IOutputService, ConsoleOutputService>();
        serviceCollection.AddTransient<IPlayerWallet, PlayerWallet>();
        serviceCollection.AddTransient<IGame, SlotGame>();
        serviceCollection.AddTransient<Random>();
    }
}
