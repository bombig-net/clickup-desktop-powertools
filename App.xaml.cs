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

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Initialize logging
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
        });

        var logger = loggerFactory.CreateLogger<AppStartup>();

        // Initialize application
        _appStartup = new AppStartup(logger);
        _appStartup.Initialize();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _appStartup?.Shutdown();
        base.OnExit(e);
    }
}

