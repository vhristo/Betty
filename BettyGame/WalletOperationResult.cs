namespace BettyGame;

/// <summary>
/// Represents the result of a player wallet operation.
/// </summary>
public class WalletOperationResult
{
    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a message describing the outcome of the operation (success or error).
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the player's balance after the operation was attempted (whether successful or not).
    /// </summary>
    public decimal CurrentBalance { get; }

    /// <summary>
    /// Initializes a new instance of the WalletOperationResult class.
    /// </summary>
    /// <param name="isSuccess">True if the operation succeeded, false otherwise.</param>
    /// <param name="message">A descriptive message about the operation's outcome.</param>
    /// <param name="currentBalance">The balance after the operation.</param>
    private WalletOperationResult(bool isSuccess, string message, decimal currentBalance)
    {
        IsSuccess = isSuccess;
        Message = message;
        CurrentBalance = currentBalance;
    }

    /// <summary>
    /// Creates a successful operation result.
    /// </summary>
    /// <param name="message">Success message.</param>
    /// <param name="currentBalance">The new balance after success.</param>
    /// <returns>A successful WalletOperationResult.</returns>
    public static WalletOperationResult Success(string message, decimal currentBalance) =>
        new WalletOperationResult(true, message, currentBalance);

    /// <summary>
    /// Creates a failed operation result.
    /// </summary>
    /// <param name="message">Error message.</param>
    /// <param name="currentBalance">The balance at the time of failure.</param>
    /// <returns>A failed WalletOperationResult.</returns>
    public static WalletOperationResult Failure(string message, decimal currentBalance) =>
        new WalletOperationResult(false, message, currentBalance);
}