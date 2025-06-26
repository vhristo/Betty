using Microsoft.Extensions.Logging;

namespace BettyGame.Wallet;

/// <summary>
/// Represents the player's financial wallet, managing deposits, withdrawals, bets, and wins.
/// Implements IPlayerWallet for decoupling.
/// </summary>
public class PlayerWallet : IPlayerWallet
{
    private readonly ILogger<PlayerWallet> _logger;

    /// <summary>
    /// Gets the current balance of the player's wallet.
    /// Using decimal for financial accuracy.
    /// </summary>
    public decimal Balance { get; private set; }

    /// <summary>
    /// Initializes a new instance of the PlayerWallet class with a starting balance of $0 and an injected logger.
    /// </summary>
    /// <param name="outputService">The output service instance to use for internal logging.</param>
    public PlayerWallet(ILogger<PlayerWallet> logger)
    {
        _logger = logger;
        Balance = 0m;
    }

    /// <summary>
    /// Deposits funds into the player's wallet.
    /// </summary>
    /// <param name="amount">The amount to deposit. Must be a positive number.</param>
    /// <returns>A WalletOperationResult indicating success or failure.</returns>
    public WalletOperationResult Deposit(decimal amount)
    {
        if (amount <= 0)
        {
            _logger.LogWarning($"Attempted to deposit non-positive amount: €{amount:F2}.");

            return WalletOperationResult.Failure("Deposit amount must be positive.", Balance);
        }

        Balance += amount;

        _logger.LogInformation($"Successfully deposited €{amount:F2}. New balance: €{Balance:F2}.");

        return WalletOperationResult.Success($"Successfully deposited €{amount:F2}.", Balance);
    }

    /// <summary>
    /// Withdraws funds from the player's wallet.
    /// </summary>
    /// <param name="amount">The amount to withdraw. Must be a positive number.</param>
    /// <returns>A WalletOperationResult indicating success or failure.</returns>
    public WalletOperationResult Withdraw(decimal amount)
    {
        if (amount <= 0)
        {
            _logger.LogInformation($"Attempted to withdraw non-positive amount: €{amount:F2}.");

            return WalletOperationResult.Failure("Withdrawal amount must be positive.", Balance);
        }

        if (Balance < amount)
        {
            _logger.LogWarning($"Insufficient funds for withdrawal. Current: €{Balance:F2}, requested: €{amount:F2}.");

            return WalletOperationResult.Failure($"Insufficient funds. Current balance: €{Balance:F2}, requested withdrawal: €{amount:F2}.", Balance);
        }

        Balance -= amount;

        _logger.LogInformation($"Successfully withdrew €{amount:F2}. New balance: €{Balance:F2}.");

        return WalletOperationResult.Success($"Successfully withdrew €{amount:F2}.", Balance);
    }

    /// <summary>
    /// Places a bet from the player's wallet.
    /// </summary>
    /// <param name="betAmount">The amount of the bet. Must be a positive number.</param>
    /// <returns>A WalletOperationResult indicating success or failure.</returns>
    public WalletOperationResult PlaceBet(decimal betAmount)
    {
        if (betAmount <= 0)
        {
            _logger.LogWarning($"Attempted to place non-positive bet: €{betAmount:F2}.");

            return WalletOperationResult.Failure("Bet amount must be positive.", Balance);
        }

        if (Balance < betAmount)
        {
            _logger.LogWarning($"Insufficient funds to place bet. Current: €{Balance:F2}, requested bet: €{betAmount:F2}.");

            return WalletOperationResult.Failure($"Insufficient funds to place bet. Current balance: €{Balance:F2}, requested bet: €{betAmount:F2}.", Balance);
        }

        Balance -= betAmount;

        _logger.LogInformation($"Successfully placed bet of €{betAmount:F2}. New balance: €{Balance:F2}.");

        return WalletOperationResult.Success($"Successfully placed bet of €{betAmount:F2}.", Balance);
    }

    /// <summary>
    /// Accepts a win amount and adds it to the player's wallet.
    /// </summary>
    /// <param name="winAmount">The amount won. Must be a non-negative number.</param>
    public void AcceptWin(decimal winAmount)
    {
        if (winAmount < 0)
        {
            // This indicates a logical error in game payout calculation.
            // In a professional system, this would be a critical error and might trigger alerts.
            _logger.LogError("Attempted to accept a negative win amount. Win ignored.");

            return;
        }

        Balance += winAmount;

        _logger.LogInformation($"Win of €{winAmount:F2} accepted. New balance: €{Balance:F2}.");
    }
}