using System;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;

namespace ClickUpDesktopPowerTools.Core;

public sealed class SimpleFileLoggerProvider : ILoggerProvider
{
    private readonly StreamWriter _writer;
    private readonly object _lockObject = new object();
    private bool _disposed = false;

    public SimpleFileLoggerProvider()
    {
        var logFilePath = GetLogFilePath();
        var logDirectory = Path.GetDirectoryName(logFilePath);
        
        if (!string.IsNullOrWhiteSpace(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
        }

        // Open file in append mode with UTF-8 encoding
        var fileStream = new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.Read);
        _writer = new StreamWriter(fileStream, Encoding.UTF8)
        {
            AutoFlush = true
        };
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new SimpleFileLogger(categoryName, _writer);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            lock (_lockObject)
            {
                if (!_disposed)
                {
                    _writer?.Flush();
                    _writer?.Dispose();
                    _disposed = true;
                }
            }
        }
    }

    private static string GetLogFilePath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "ClickUpDesktopPowerTools", "logs", "app.log");
    }
}

