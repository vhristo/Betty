using Microsoft.Extensions.Logging;

namespace BettyGame;

/// <summary>
/// An ILogger implementation that writes messages to the console.
/// </summary>
public class ConsoleLogger : ILogger
{
    private readonly string _name;
    private readonly Func<string, LogLevel, bool> _filter; // Filter function from provider

    public ConsoleLogger(string name, Func<string, LogLevel, bool> filter)
    {
        _name = name;
        _filter = filter;
    }

    private void WriteLog(string level, string message, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [{level.ToUpper()}] {message}");
        Console.ResetColor(); // Reset color after writing
    }

    public void LogInformation(string message)
    {
        WriteLog("Info", message, ConsoleColor.White);
    }

    public void LogWarning(string message)
    {
        WriteLog("Warning", message, ConsoleColor.Yellow);
    }

    public void LogError(string message, Exception exception = null)
    {
        WriteLog("Error", message + (exception != null ? $" Exception: {exception.Message}" : ""), ConsoleColor.Red);
        if (exception != null)
        {
            // In a real application, you might want to log the full stack trace to a file
            // but only a summary to console for clarity, or control this via log level.
            Console.WriteLine(exception.StackTrace);
        }
    }

    public void LogDebug(string message)
    {
        // Debug messages might be conditionally compiled or filtered in a real app
        // For now, we'll just write them.
        WriteLog("Debug", message, ConsoleColor.Gray);
    }

    // This method determines if a log message at a given level should be processed.
    public bool IsEnabled(LogLevel logLevel)
    {
        if (logLevel == LogLevel.None)
        {
            return false;
        }
        // Use the filter provided by the logger provider (which gets it from configuration)
        return _filter?.Invoke(_name, logLevel) ?? true; // Default to true if no filter provided
    }

    // This method is for creating logging scopes. For a simple console logger,
    // we might not implement complex scope handling, but the interface requires it.
    public IDisposable BeginScope<TState>(TState state)
    {
        // For a basic console logger, we might just print the scope information
        // or do nothing. More advanced loggers would push this state onto a context stack.
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [SCOPE] {_name}: {state}");
        Console.ResetColor();
        return null; // Returning null is allowed if no disposable resource is needed
    }

    // This is the core logging method that all LogInformation, LogError, etc. call.
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return; // Do nothing if the log level is not enabled
        }

        // Determine console color based on log level
        ConsoleColor originalColor = Console.ForegroundColor;
        ConsoleColor color = logLevel switch
        {
            LogLevel.Trace => ConsoleColor.DarkGray,
            LogLevel.Debug => ConsoleColor.Gray,
            LogLevel.Information => ConsoleColor.White,
            LogLevel.Warning => ConsoleColor.Yellow,
            LogLevel.Error => ConsoleColor.Red,
            LogLevel.Critical => ConsoleColor.DarkRed,
            _ => ConsoleColor.White,
        };

        Console.ForegroundColor = color;

        // Format the message using the provided formatter function
        string message = formatter(state, exception);
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [{logLevel.ToString().ToUpper()}] [{_name}] {message}");

        if (exception != null)
        {
            Console.WriteLine(exception.ToString()); // Print full exception details
        }

        Console.ForegroundColor = originalColor; // Reset to original color
    }
}