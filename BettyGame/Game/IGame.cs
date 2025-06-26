namespace BettyGame.Game;

/// <summary>
/// Defines the contract for a game of chance.
/// </summary>
public interface IGame
{
    /// <summary>
    /// Gets the minimum allowed bet amount for this game.
    /// </summary>
    decimal MinBet { get; }

    /// <summary>
    /// Gets the maximum allowed bet amount for this game.
    /// </summary>
    decimal MaxBet { get; }

    /// <summary>
    /// Simulates playing one round of the game.
    /// </summary>
    /// <param name="betAmount">The amount of money the player bet.</param>
    /// <returns>The amount won (0 for a loss).</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the bet amount is outside the allowed range.</exception>
    decimal Play(decimal betAmount);
}