using BettyGame.Config;
using BettyGame.Game;
using Microsoft.Extensions.Logging;
using Moq;

namespace BettyGame.Tests.UnitTests;

public class SlotGameTests
{
    private readonly Mock<ILogger<SlotGame>> _mockLogger;
    private readonly GameSettings _defaultSettings;

    public SlotGameTests()
    {
        _mockLogger = new Mock<ILogger<SlotGame>>();
        _defaultSettings = new GameSettings
        {
            MinBet = 1.0m,
            MaxBet = 10.0m,
            LossChance = 0.50,      // 50% chance of loss
            WinX2Chance = 0.40,     // 40% chance of 2x win (0.50 to 0.90)
            WinX2ToX10Chance = 0.10 // 10% chance of 2x-10x win (0.90 to 1.00)
        };
    }

    [Theory]
    [InlineData(0.51, 2.0)] // Just above loss chance, should be 2x
    [InlineData(0.89, 2.0)] // Just below 2x-10x chance, should be 2x
    public void Play_ReturnsTwoXWin_WhenRandomFallsInWinX2Range(double randomValue, decimal expectedMultiplier)
    {
        // Arrange
        var mockRandom = new Mock<Random>();
        // Use SetupSequence to control multiple calls if needed, or Setup for single call
        mockRandom.Setup(r => r.NextDouble()).Returns(randomValue);

        var game = new SlotGame(mockRandom.Object, _defaultSettings, _mockLogger.Object);
        decimal bet = 5m;

        // Act
        decimal winAmount = game.Play(bet);

        // Assert
        Assert.Equal(bet * expectedMultiplier, winAmount);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Bet €{bet:F2} resulted in a 2x win (€{bet * expectedMultiplier:F2}).")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.AtLeastOnce);
    }

    [Theory]
    [InlineData(0.49)] // Just below loss chance, should be loss
    [InlineData(0.00)] // Lowest possible, should be loss
    public void Play_ReturnsZeroWin_WhenRandomFallsInLossRange(double randomValue)
    {
        // Arrange
        var mockRandom = new Mock<Random>();
        mockRandom.Setup(r => r.NextDouble()).Returns(randomValue);

        var game = new SlotGame(mockRandom.Object, _defaultSettings, _mockLogger.Object);
        decimal bet = 5m;

        // Act
        decimal winAmount = game.Play(bet);

        // Assert
        Assert.Equal(0m, winAmount);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Bet €{bet:F2} resulted in a loss.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData(0.91, 500, 5.00)] // random value in 2x-10x range, Next(201,1001) returns 500 -> 5x
    [InlineData(0.99, 1000, 10.00)] // random value in 2x-10x range, Next(201,1001) returns 1000 -> 10x
    public void Play_ReturnsMultiXWin_WhenRandomFallsInWinX2ToX10Range(double nextDoubleValue, int nextIntReturnValue, decimal expectedMultiplier)
    {
        // Arrange
        var mockRandom = new Mock<Random>();
        mockRandom.SetupSequence(r => r.NextDouble()) // First call for outcome
                  .Returns(nextDoubleValue);
        mockRandom.SetupSequence(r => r.Next(It.IsAny<int>(), It.IsAny<int>())) // Second call for multiplier (after nextDouble for 2x-10x)
                  .Returns(nextIntReturnValue);

        var game = new SlotGame(mockRandom.Object, _defaultSettings, _mockLogger.Object);
        decimal bet = 5m;

        // Act
        decimal winAmount = game.Play(bet);

        // Assert
        Assert.Equal(bet * expectedMultiplier, winAmount);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Bet €{bet:F2} resulted in a {expectedMultiplier:F2}x win (€{bet * expectedMultiplier:F2}).")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }


    [Theory]
    [InlineData(1.5)] // min bet
    [InlineData(10.0)] // max bet
    public void Play_ValidBetAmount_DoesNotThrowException(decimal betAmount)
    {
        // Arrange
        var mockRandom = new Mock<Random>();
        mockRandom.Setup(r => r.NextDouble()).Returns(1.5); // Any valid random value
        var game = new SlotGame(mockRandom.Object, _defaultSettings, _mockLogger.Object);

        // Act & Assert
        // Assert.DoesNotThrow is generally avoided in favor of directly asserting the outcome,
        // but for boundary checks on input it can be useful.
        var exception = Record.Exception(() => game.Play(betAmount));
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(0.9)] // Below min bet
    [InlineData(10.1)] // Above max bet
    public void Play_InvalidBetAmount_ThrowsArgumentOutOfRangeException(decimal betAmount)
    {
        // Arrange
        var mockRandom = new Mock<Random>();
        var game = new SlotGame(mockRandom.Object, _defaultSettings, _mockLogger.Object);

        // Act & Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => game.Play(betAmount));
        Assert.Contains("Bet amount must be between", ex.Message); // Check for part of the message
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Invalid bet amount €{betAmount:F2} received.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
}
