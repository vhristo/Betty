using BettyGame.Config;
using BettyGame.Game;
using BettyGame.Wallet;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;


namespace BettyGame.Tests.IntegrationTests;

public class AppIntegrationTests
{
    // Use NullLogger for integration tests if you don't need to assert on log output
    private readonly ILogger<PlayerWallet> _walletLogger = new NullLogger<PlayerWallet>();
    private readonly ILogger<SlotGame> _slotGameLogger = new NullLogger<SlotGame>();

    private readonly GameSettings _gameSettings;

    public AppIntegrationTests()
    {
        _gameSettings = new GameSettings
        {
            MinBet = 1.0m,
            MaxBet = 10.0m,
            LossChance = 0.50,
            WinX2Chance = 0.40,
            WinX2ToX10Chance = 0.10
        };
    }

    [Fact]
    public void FullGameFlow_PlayerLoses_BalanceDecreasesByBet()
    {
        // Arrange
        var wallet = new PlayerWallet(_walletLogger);
        // Use a mock Random for deterministic outcome in integration test if needed for specific scenarios
        // Or use a real Random if you want more realistic, but less deterministic, integration tests.
        // For a 'player loses' scenario, we'll mock Random.
        var mockRandom = new Mock<Random>();
        mockRandom.Setup(r => r.NextDouble()).Returns(0.1); // Ensures a loss (falls within LossChance = 0.50)

        var slotGame = new SlotGame(mockRandom.Object, _gameSettings, _slotGameLogger);

        decimal initialDeposit = 100m;
        decimal betAmount = 10m;

        // Act
        wallet.Deposit(initialDeposit);
        var betResult = wallet.PlaceBet(betAmount);
        decimal winAmount = 0m; // Initialize
        if (betResult.IsSuccess)
        {
            winAmount = slotGame.Play(betAmount);
            wallet.AcceptWin(winAmount);
        }

        // Assert
        Assert.True(betResult.IsSuccess);
        Assert.Equal(0m, winAmount); // Should be a loss
        Assert.Equal(initialDeposit - betAmount, wallet.Balance); // Balance should be initial - bet
    }

    [Fact]
    public void FullGameFlow_PlayerWins2X_BalanceIncreasesCorrectly()
    {
        // Arrange
        var wallet = new PlayerWallet(_walletLogger);
        var mockRandom = new Mock<Random>();
        mockRandom.Setup(r => r.NextDouble()).Returns(0.6); // Ensures a 2x win (falls between 0.50 and 0.90)

        var slotGame = new SlotGame(mockRandom.Object, _gameSettings, _slotGameLogger);

        decimal initialDeposit = 100m;
        decimal betAmount = 10m;

        // Act
        wallet.Deposit(initialDeposit);
        var betResult = wallet.PlaceBet(betAmount);
        decimal winAmount = 0m;
        if (betResult.IsSuccess)
        {
            winAmount = slotGame.Play(betAmount);
            wallet.AcceptWin(winAmount);
        }

        // Assert
        Assert.True(betResult.IsSuccess);
        Assert.Equal(betAmount * 2m, winAmount); // Should be a 2x win
        Assert.Equal(initialDeposit - betAmount + winAmount, wallet.Balance); // Balance: initial - bet + win
    }

    [Fact]
    public void FullGameFlow_PlayerWins5X_BalanceIncreasesCorrectly()
    {
        // Arrange
        var wallet = new PlayerWallet(_walletLogger);
        var mockRandom = new Mock<Random>();
        mockRandom.SetupSequence(r => r.NextDouble())
                  .Returns(0.95); // First call for outcome: ensures 2x-10x win (0.90 to 1.00)
        mockRandom.SetupSequence(r => r.Next(It.IsAny<int>(), It.IsAny<int>()))
                  .Returns(500); // Second call for multiplier: 500 -> 5x (500 / 100 = 5)

        var slotGame = new SlotGame(mockRandom.Object, _gameSettings, _slotGameLogger);

        decimal initialDeposit = 100m;
        decimal betAmount = 10m;
        decimal expectedWinAmount = betAmount * 5m;

        // Act
        wallet.Deposit(initialDeposit);
        var betResult = wallet.PlaceBet(betAmount);
        decimal winAmount = 0m;
        if (betResult.IsSuccess)
        {
            winAmount = slotGame.Play(betAmount);
            wallet.AcceptWin(winAmount);
        }

        // Assert
        Assert.True(betResult.IsSuccess);
        Assert.Equal(expectedWinAmount, winAmount); // Should be a 5x win
        Assert.Equal(initialDeposit - betAmount + winAmount, wallet.Balance); // Balance: initial - bet + win
    }

    [Fact]
    public void FullGameFlow_InsufficientFundsForBet_BalanceRemainsUnchanged()
    {
        // Arrange
        var wallet = new PlayerWallet(_walletLogger);
        var slotGame = new SlotGame(new Random(), _gameSettings, _slotGameLogger); // Random doesn't matter here

        decimal initialDeposit = 5m;
        decimal betAmount = 10m; // Bet is greater than initial deposit

        // Act
        wallet.Deposit(initialDeposit);
        var betResult = wallet.PlaceBet(betAmount); // This should fail

        // Assert
        Assert.False(betResult.IsSuccess);
        Assert.Contains("Insufficient funds to place bet", betResult.Message);
        Assert.Equal(initialDeposit, wallet.Balance); // Balance should not have changed
    }

    [Fact]
    public void FullGameFlow_InvalidBetAmountToGame_BetIsRefunded()
    {
        // Arrange
        var wallet = new PlayerWallet(_walletLogger);
        var mockSlotGameLogger = new Mock<ILogger<SlotGame>>(); // Mock logger to check for error logs
        var slotGame = new SlotGame(new Random(), _gameSettings, mockSlotGameLogger.Object);

        decimal initialDeposit = 100m;
        decimal betAmount = 5m; // Valid for wallet, but we'll simulate invalid for game
        decimal invalidGameBetAmount = 0m; // Simulates an invalid bet passed to Play()

        // Act
        wallet.Deposit(initialDeposit);
        var betResult = wallet.PlaceBet(betAmount); // Wallet deducts bet
        Assert.True(betResult.IsSuccess);
        Assert.Equal(initialDeposit - betAmount, wallet.Balance);

        // Simulate the game being called with an invalid amount causing an exception
        // We're deliberately calling Play with a bad value here, even if PlaceBet succeeded.
        // This tests the error handling path where the bet is refunded.
        try
        {
            slotGame.Play(invalidGameBetAmount); // This will throw ArgumentOutOfRangeException
        }
        catch (ArgumentOutOfRangeException)
        {
            wallet.AcceptWin(betAmount); // Simulate refund logic from Program.cs
        }

        // Assert
        // Wallet balance should be back to initial, effectively a refund
        Assert.Equal(initialDeposit, wallet.Balance);
        // Verify that SlotGame logged a warning about the invalid bet amount
        mockSlotGameLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Invalid bet amount €{invalidGameBetAmount:F2} received.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
}