namespace BettyGame.Wallet;

/// <summary>
/// Defines the contract for a player's financial wallet.
/// </summary>
public interface IPlayerWallet
{
    /// <summary>
    /// Gets the current balance of the player's wallet.
    /// </summary>
    decimal Balance { get; }

    /// <summary>
    /// Deposits funds into the player's wallet.
    /// </summary>
    /// <param name="amount">The amount to deposit. Must be a positive number.</param>
    /// <returns>A WalletOperationResult indicating success or failure.</returns>
    WalletOperationResult Deposit(decimal amount);

    /// <summary>
    /// Withdraws funds from the player's wallet.
    /// </summary>
    /// <param name="amount">The amount to withdraw. Must be a positive number.</param>
    /// <returns>A WalletOperationResult indicating success or failure.</returns>
    WalletOperationResult Withdraw(decimal amount);

    /// <summary>
    /// Places a bet from the player's wallet.
    /// </summary>
    /// <param name="betAmount">The amount of the bet. Must be a positive number.</param>
    /// <returns>A WalletOperationResult indicating success or failure.</returns>
    WalletOperationResult PlaceBet(decimal betAmount);

    /// <summary>
    /// Accepts a win amount and adds it to the player's wallet.
    /// </summary>
    /// <param name="winAmount">The amount won. Must be a non-negative number.</param>
    void AcceptWin(decimal winAmount);
}