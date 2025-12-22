using Microsoft.Extensions.Logging;

namespace ClickUpDesktopPowerTools.Core;

public class AppStartup
{
    private readonly ILogger<AppStartup> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private TrayHost? _trayHost;
    private TokenStorage? _tokenStorage;
    private ClickUpApi? _clickUpApi;

    public AppStartup(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<AppStartup>();
    }

    public void Initialize()
    {
        _logger.LogInformation("Initializing ClickUp Desktop PowerTools");

        // Create token storage and API (kept as fields for future use)
        _tokenStorage = new TokenStorage();
        var apiLogger = _loggerFactory.CreateLogger<ClickUpApi>();
        _clickUpApi = new ClickUpApi(_tokenStorage, apiLogger);

        // Initialize tray host
        _trayHost = new TrayHost();
        _trayHost.Initialize();

        _logger.LogInformation("Tray host initialized");
    }

    public void Shutdown()
    {
        _trayHost?.Dispose();
        _logger.LogInformation("Application shutting down");
    }
}
