using BettyGame.Wallet;
using Microsoft.Extensions.Logging;
using Moq;

namespace BettyGame.Tests.UnitTests;

public class PlayerWalletTests
{
    private readonly Mock<ILogger<PlayerWallet>> _mockLogger;

    public PlayerWalletTests()
    {
        _mockLogger = new Mock<ILogger<PlayerWallet>>();
    }

    [Fact]
    public void PlayerWallet_InitialBalanceIsZero()
    {
        // Arrange
        var wallet = new PlayerWallet(_mockLogger.Object);

        // Act & Assert
        Assert.Equal(0m, wallet.Balance);
    }

    [Theory]
    [InlineData(100.0)]
    [InlineData(0.01)]
    [InlineData(1000000.0)]
    public void Deposit_ValidAmount_IncreasesBalance(decimal amount)
    {
        // Arrange
        var wallet = new PlayerWallet(_mockLogger.Object);
        var initialBalance = wallet.Balance;
    
        // Act
        var result = wallet.Deposit(amount);
    
        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(initialBalance + amount, wallet.Balance);
        Assert.Contains($"Successfully deposited €{amount:F2}", result.Message);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Successfully deposited €{amount:F2}. New balance: €{wallet.Balance:F2}.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Deposit_NonPositiveAmount_ReturnsFailure(decimal amount)
    {
        // Arrange
        var wallet = new PlayerWallet(_mockLogger.Object);
        var initialBalance = wallet.Balance;

        // Act
        var result = wallet.Deposit(amount);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(initialBalance, wallet.Balance); // Balance should not change
        Assert.Contains("Deposit amount must be positive.", result.Message);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning, // Expect a warning log
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Attempted to deposit non-positive amount: €{amount:F2}.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData(10.0, 5.0)]
    [InlineData(50.0, 50.0)]
    public void Withdraw_ValidAmount_DecreasesBalance(decimal depositAmount, decimal withdrawAmount)
    {
        // Arrange
        var wallet = new PlayerWallet(_mockLogger.Object);
        wallet.Deposit(depositAmount); // Pre-deposit funds (this will also generate logs, so we need to adjust verification counts or reset mock)
        _mockLogger.Invocations.Clear(); // Clear previous invocations for cleaner test

        var initialBalance = wallet.Balance;

        // Act
        var result = wallet.Withdraw(withdrawAmount);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(initialBalance - withdrawAmount, wallet.Balance);
        Assert.Contains($"Successfully withdrew €{withdrawAmount:F2}", result.Message);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Successfully withdrew €{withdrawAmount:F2}. New balance: €{wallet.Balance:F2}.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void Withdraw_InsufficientFunds_ReturnsFailure()
    {
        // Arrange
        var wallet = new PlayerWallet(_mockLogger.Object);
        wallet.Deposit(10m); // Only $10
        _mockLogger.Invocations.Clear();

        // Act
        var result = wallet.Withdraw(20m);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(10m, wallet.Balance); // Balance should not change
        Assert.Contains("Insufficient funds.", result.Message);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Insufficient funds for withdrawal.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData(100.0, 10.0)]
    [InlineData(50.0, 50.0)]
    public void PlaceBet_ValidAmount_DecreasesBalance(decimal depositAmount, decimal betAmount)
    {
        // Arrange
        var wallet = new PlayerWallet(_mockLogger.Object);
        wallet.Deposit(depositAmount);
        _mockLogger.Invocations.Clear();

        var initialBalance = wallet.Balance;

        // Act
        var result = wallet.PlaceBet(betAmount);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(initialBalance - betAmount, wallet.Balance);
        Assert.Contains($"Successfully placed bet of €{betAmount:F2}", result.Message);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Successfully placed bet of €{betAmount:F2}. New balance: €{wallet.Balance:F2}.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void PlaceBet_InsufficientFunds_ReturnsFailure()
    {
        // Arrange
        var wallet = new PlayerWallet(_mockLogger.Object);
        wallet.Deposit(5m); // Only $5
        _mockLogger.Invocations.Clear();

        // Act
        var result = wallet.PlaceBet(10m);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(5m, wallet.Balance); // Balance should not change
        Assert.Contains("Insufficient funds to place bet.", result.Message);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Insufficient funds to place bet.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData(10.0)]
    [InlineData(0.01)]
    public void AcceptWin_PositiveAmount_IncreasesBalance(decimal winAmount)
    {
        // Arrange
        var wallet = new PlayerWallet(_mockLogger.Object);
        wallet.Deposit(10m);
        _mockLogger.Invocations.Clear();

        var initialBalance = wallet.Balance;

        // Act
        wallet.AcceptWin(winAmount);

        // Assert
        Assert.Equal(initialBalance + winAmount, wallet.Balance);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Win of €{winAmount:F2} accepted. New balance: €{wallet.Balance:F2}.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void AcceptWin_NegativeAmount_LogsErrorAndDoesNotChangeBalance()
    {
        // Arrange
        var wallet = new PlayerWallet(_mockLogger.Object);
        wallet.Deposit(100m);
        _mockLogger.Invocations.Clear();

        var initialBalance = wallet.Balance;

        // Act
        wallet.AcceptWin(-10m);

        // Assert
        Assert.Equal(initialBalance, wallet.Balance); // Balance should not change
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error, // Expect an error log
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Attempted to accept a negative win amount. Win ignored.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
}