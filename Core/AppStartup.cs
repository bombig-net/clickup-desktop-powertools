using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace ClickUpDesktopPowerTools.Core;

public class AppStartup
{
    private readonly ILogger<AppStartup> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private TrayHost? _trayHost;
    private TokenStorage? _tokenStorage;
    private ClickUpApi? _clickUpApi;
    private ClickUpRuntime? _clickUpRuntime;
    private CoreState? _coreState;

    public AppStartup(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<AppStartup>();
    }

    public void Initialize()
    {
        _logger.LogInformation("Initializing ClickUp Desktop PowerTools");

        // Create token storage and API
        _tokenStorage = new TokenStorage();
        var apiLogger = _loggerFactory.CreateLogger<ClickUpApi>();
        _clickUpApi = new ClickUpApi(_tokenStorage, apiLogger);

        // Create runtime detection
        _clickUpRuntime = new ClickUpRuntime(_loggerFactory.CreateLogger<ClickUpRuntime>());

        // Compute log file path (matches SimpleFileLoggerProvider)
        var logFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ClickUpDesktopPowerTools", "logs", "app.log");

        // Create core state with runtime info
        _coreState = new CoreState
        {
            HasApiToken = !string.IsNullOrEmpty(_tokenStorage.GetToken()),
            LogFilePath = logFilePath,
            ClickUpDesktopStatus = _clickUpRuntime.CheckStatus()
        };

        _logger.LogInformation("Core version: {Version}, .NET: {DotNet}, ClickUp Desktop: {Status}",
            _coreState.Version, _coreState.DotNetVersion, _coreState.ClickUpDesktopStatus);

        // Initialize tray host with dependencies
        _trayHost = new TrayHost(_tokenStorage, _clickUpApi, _coreState, _clickUpRuntime, _loggerFactory);
        _trayHost.Initialize();

        _logger.LogInformation("Tray host initialized");
    }

    public void Shutdown()
    {
        _trayHost?.Dispose();
        _logger.LogInformation("Application shutting down");
    }
}
