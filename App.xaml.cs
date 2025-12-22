using System;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;
using ClickUpDesktopPowerTools.Core;

namespace ClickUpDesktopPowerTools;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private static Mutex? _instanceMutex;
    private const string MutexName = "ClickUpDesktopPowerTools_SingleInstance";

    private AppStartup? _appStartup;
    private ILoggerFactory? _loggerFactory;
    private ILogger<App>? _logger;

    protected override void OnStartup(StartupEventArgs e)
    {
        // 1. Single instance check - MUST be first
        _instanceMutex = new Mutex(true, MutexName, out bool createdNew);
        if (!createdNew)
        {
            Shutdown();
            return;
        }

        base.OnStartup(e);

        // 2. Initialize logging
        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddProvider(new SimpleFileLoggerProvider());
        });
        _logger = _loggerFactory.CreateLogger<App>();

        // 3. Register exception handlers (they need the logger)
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        // 4. Initialize application
        _appStartup = new AppStartup(_loggerFactory);
        _appStartup.Initialize();
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        _logger?.LogCritical(e.Exception, "Unhandled UI thread exception");
        // Do NOT set e.Handled = true - let the app crash rather than continue in corrupt state
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;
        _logger?.LogCritical(exception, "Unhandled background thread exception (IsTerminating: {IsTerminating})", e.IsTerminating);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _appStartup?.Shutdown();
        _loggerFactory?.Dispose();
        _instanceMutex?.Dispose();
        base.OnExit(e);
    }
}

