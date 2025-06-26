using BettyGame.Config;
using Microsoft.Extensions.Logging;

namespace BettyGame.Game;

/// <summary>
/// Represents a simple slot game with predefined win/loss probabilities.
/// Implements IGame for decoupling and uses injected GameSettings.
/// </summary>
public class SlotGame : IGame
{
    private readonly Random _random;
    private readonly GameSettings _settings;
    private readonly ILogger _logger; // Added ILogger dependency

    // IGame properties, now drawing from injected settings
    public decimal MinBet => _settings.MinBet;
    public decimal MaxBet => _settings.MaxBet;

    /// <summary>
    /// Initializes a new instance of the SlotGame class with a provided Random instance, game settings, and logger.
    /// </summary>
    /// <param name="random">The Random instance to use for outcome generation.</param>
    /// <param name="settings">The game configuration settings.</param>
    /// <param name="logger">The logger instance to use for internal logging.</param>
    public SlotGame(Random random, GameSettings settings, ILogger logger)
    {
        _random = random ?? throw new ArgumentNullException(nameof(random));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("Slot game initialized with provided settings.");
        _logger.LogDebug($"Game rules: MinBet={MinBet}, MaxBet={MaxBet}, Loss={settings.LossChance}, WinX2={settings.WinX2Chance}, WinX2-X10={settings.WinX2ToX10Chance}");
    }

    /// <summary>
    /// Simulates playing one round of the slot game.
    /// </summary>
    /// <param name="betAmount">The amount of money the player bet.</param>
    /// <returns>The amount won (0 for a loss).</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the bet amount is outside the allowed range.</exception>
    public decimal Play(decimal betAmount)
    {
        _logger.LogDebug($"Attempting to play slot game with bet: €{betAmount:F2}");

        if (betAmount < _settings.MinBet || betAmount > _settings.MaxBet)
        {
            // This validation should ideally be caught by the caller (Program.cs) before this method is called,
            // but it's good to have for robustness and to log internal inconsistencies.
            _logger.LogWarning($"Invalid bet amount €{betAmount:F2} received. Range is €{MinBet:F2}-€{MaxBet:F2}.");
            throw new ArgumentOutOfRangeException(
                nameof(betAmount),
                $"Bet amount must be between €{_settings.MinBet:F2} and €{_settings.MaxBet:F2}."
            );
        }

        double outcomeRoll = _random.NextDouble(); // Value between 0.0 (inclusive) and 1.0 (exclusive)
        _logger.LogDebug($"Outcome roll: {outcomeRoll:F4}");

        decimal winAmount;
        if (outcomeRoll < _settings.LossChance)
        {
            winAmount = 0m;
            _logger.LogInformation($"Bet €{betAmount:F2} resulted in a loss.");
        }
        else if (outcomeRoll < _settings.LossChance + _settings.WinX2Chance)
        {
            // 40% chance to win exactly 2x
            winAmount = betAmount * 2m;
            _logger.LogInformation($"Bet €{betAmount:F2} resulted in a 2x win (€{winAmount:F2}).");
        }
        else // Remaining 10% chance to win between 2x and 10x
        {
            // Generate a multiplier between 2.001 and 10.000 (inclusive of 10.000)
            int multiplierNumerator = _random.Next(201, 1001); // Random int from 201 to 1000
            decimal multiplier = (decimal)multiplierNumerator / 100m;

            winAmount = betAmount * multiplier;
            _logger.LogInformation($"Bet €{betAmount:F2} resulted in a {multiplier:F2}x win (€{winAmount:F2}).");
        }

        return winAmount;
    }
}