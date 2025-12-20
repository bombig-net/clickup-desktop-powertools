using System;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;

namespace ClickUpDesktopPowerTools.Core;

internal sealed class SimpleFileLogger : ILogger
{
    private readonly string _categoryName;
    private readonly StreamWriter _writer;
    private readonly object _lockObject = new object();

    public SimpleFileLogger(string categoryName, StreamWriter writer)
    {
        _categoryName = categoryName;
        _writer = writer;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel)
    {
        // Log Information, Warning, and Error by default
        // Debug can be enabled by changing this condition
        return logLevel >= LogLevel.Information;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        try
        {
            var message = formatter(state, exception);
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var level = logLevel.ToString().ToUpperInvariant().PadRight(5);

            var logEntry = new StringBuilder();
            logEntry.Append($"[{timestamp}] [{level}] {_categoryName}: {message}");

            if (exception != null)
            {
                logEntry.AppendLine();
                logEntry.Append($"Exception: {exception.GetType().Name}");
                logEntry.AppendLine();
                logEntry.Append($"Message: {exception.Message}");
                logEntry.AppendLine();
                logEntry.Append($"Stack Trace: {exception.StackTrace}");
            }

            lock (_lockObject)
            {
                _writer.WriteLine(logEntry.ToString());
                _writer.Flush();
            }
        }
        catch
        {
            // Don't crash the application if logging fails
        }
    }
}

