using BettyGame.Config;
using BettyGame.Game;
using BettyGame.Infrastructure;
using BettyGame.Services;
using BettyGame.Wallet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BettyGame;

public class Program
{
    private static IPlayerWallet _wallet;
    private static IGame _slotGame;
    private static ILogger<Program> _logger;
    private static IOutputService _outputService;

    public static void Main(string[] args)
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var serviceCollection = new ServiceCollection();
        ServiceCollectionExtensions.ConfigureServices(serviceCollection, configuration);

        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
        _outputService = serviceProvider.GetRequiredService<IOutputService>();
        _logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        _wallet = serviceProvider.GetRequiredService<IPlayerWallet>();
        _slotGame = serviceProvider.GetRequiredService<IGame>();

        if (serviceProvider.GetService<GameSettings>() == null)
        {
            _outputService.WriteError("Error: GameSettings section not found or could not be loaded from appsettings.json. Exiting.");

            return;
        }

        DisplayWelcomeText();
        DisplayBalance();
        RunApplicationLoop();
        DisplayGoodbyeText();
    }

    private static void RunApplicationLoop()
    {
        bool exitApp = false;
        while (!exitApp)
        {
            DisplayMenu();
            string? choice = Console.ReadLine();

            switch (choice?.ToLowerInvariant())
            {
                case "d":
                    HandleDeposit();
                    break;
                case "w":
                    HandleWithdrawal();
                    break;
                case "p":
                    HandlePlayGame();
                    break;
                case "e":
                    exitApp = true;
                    break;
                default:
                    _logger.LogWarning("Invalid choice. Please try again.");
                    break;
            }
            _logger.LogInformation("\n-------------------------------------");
            DisplayBalance(); // Always display balance after each operation
            _logger.LogInformation("-------------------------------------\n");
        }
    }

    private static void DisplayWelcomeText()
    {
        _outputService.WriteLine("===============================================");
        _outputService.WriteLine("| Welcome to Betty's Player Wallet App 🤑🤑🤑 |");
        _outputService.WriteLine("===============================================\n");
    }

    private static void DisplayGoodbyeText()
    {
        _outputService.WriteLine("\n=====================================");
        _outputService.WriteLine("| Thank you for using the wallet app! |");
        _outputService.WriteLine("=====================================\n");
    }

    private static void DisplayBalance()
    {
        _outputService.WriteInfoVerbose($"Current Balance: €{_wallet.Balance:F2}");
    }

    private static void DisplayMenu()
    {
        _outputService.WriteLine("Please choose an option:");
        _outputService.WriteLine(" D - Deposit Funds");
        _outputService.WriteLine(" W - Withdraw Funds");
        _outputService.WriteLine($" P - Play Slot Game (Bet: €{_slotGame.MinBet:F2}-€{_slotGame.MaxBet:F2})");
        _outputService.WriteLine(" E - Exit");
        _outputService.Write("Enter your choice: ");
    }

    private static void HandleDeposit()
    {
        _outputService.Write("Enter amount to deposit: ");
        if (TryReadDecimalInput(out decimal amount))
        {
            WalletOperationResult result = _wallet.Deposit(amount);
            if (result.IsSuccess)
            {
                _logger.LogInformation(result.Message);
            }
            else
            {
                _logger.LogError($"Operation Failed: {result.Message}");
            }
        }
        else
        {
            _logger.LogWarning("Deposit amount input was invalid.");
        }
    }

    private static void HandleWithdrawal()
    {
        _outputService.Write("Enter amount to withdraw: ");

        if (TryReadDecimalInput(out decimal amount))
        {
            WalletOperationResult result = _wallet.Withdraw(amount);
            if (result.IsSuccess)
            {
                _outputService.WriteSuccess(result.Message);
            }
            else
            {
                _outputService.WriteError($"Operation Failed: {result.Message}");
            }
        }
        else
        {
            _outputService.WriteWarning("Withdrawal amount input was invalid.");
        }
    }

    private static void HandlePlayGame()
    {
        _outputService.Write($"Enter bet amount (€{_slotGame.MinBet:F2}-€{_slotGame.MaxBet:F2}): ");
        if (TryReadDecimalInput(out decimal betAmount))
        {
            if (betAmount < _slotGame.MinBet || betAmount > _slotGame.MaxBet)
            {
                _outputService.WriteWarning($"Bet amount (€{betAmount:F2}) outside valid range.");
                _outputService.WriteError($"Error: Bet amount must be between €{_slotGame.MinBet:F2} and €{_slotGame.MaxBet:F2}.");

                return;
            }

            WalletOperationResult betResult = _wallet.PlaceBet(betAmount);
            if (betResult.IsSuccess)
            {
                _outputService.WriteInfo(betResult.Message);
                try
                {
                    decimal winAmount = _slotGame.Play(betAmount);
                    _wallet.AcceptWin(winAmount);

                    if (winAmount > 0)
                    {
                        _outputService.WriteSuccess($"Congratulations! You bet €{betAmount:F2} and won €{winAmount:F2}!");
                    }
                    else
                    {
                        _outputService.WriteInfo($"Unlucky! You bet €{betAmount:F2} and lost. Better luck next time!");
                    }
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    _logger.LogError($"Game Error: Invalid bet amount passed to game logic after wallet processing. {ex.Message}", ex);
                    _wallet.AcceptWin(betAmount); // Refund
                    _outputService.WriteError($"Bet of €{betAmount:F2} refunded due to game logic error.");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"An unexpected error occurred during game play: {ex.Message}", ex);
                    _wallet.AcceptWin(betAmount);
                    _outputService.WriteError($"Bet of €{betAmount:F2} refunded due to unexpected error.");
                }
            }
            else
            {
                _outputService.WriteError($"Operation Failed: {betResult.Message}");
            }
        }
        else
        {
            _outputService.WriteWarning("Bet amount input was invalid.");
        }
    }

    private static bool TryReadDecimalInput(out decimal result)
    {
        string? input = Console.ReadLine();
        if (decimal.TryParse(input, out result) && result >= 0)
        {
            return true;
        }

        _outputService.WriteError($"Invalid numeric input received: '{input}'");
        _outputService.WriteError("Invalid input. Please enter a positive numeric value.");
        result = 0m;

        return false;
    }
}