using System.Windows;
using Microsoft.Extensions.Logging;
using ClickUpDesktopPowerTools.Core;

namespace ClickUpDesktopPowerTools;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private AppStartup? _appStartup;
    private ILoggerFactory? _loggerFactory;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Initialize logging
        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddProvider(new SimpleFileLoggerProvider());
        });

        // Initialize application
        _appStartup = new AppStartup(_loggerFactory);
        _appStartup.Initialize();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _appStartup?.Shutdown();
        _loggerFactory?.Dispose();
        base.OnExit(e);
    }
}

