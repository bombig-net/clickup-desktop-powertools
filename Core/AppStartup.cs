using Microsoft.Extensions.Logging;
using ClickUpDesktopPowerTools.Core;
using ClickUpDesktopPowerTools.Tools.TimeTracking;

namespace ClickUpDesktopPowerTools.Core;

public class AppStartup
{
    private readonly ILogger<AppStartup> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private OverlayHost? _overlayHost;
    private TimeTrackingService? _timeTrackingService;

    public AppStartup(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<AppStartup>();
    }

    public void Initialize()
    {
        _logger.LogInformation("Initializing ClickUp Desktop PowerTools");

        _overlayHost = new OverlayHost();
        
        // Create token storage and API
        var tokenStorage = new TokenStorage();
        var apiLogger = _loggerFactory.CreateLogger<ClickUpApi>();
        var clickUpApi = new ClickUpApi(tokenStorage, apiLogger);
        
        // Create TimeTracking tool (service loads team ID from settings and starts polling automatically)
        var timeTrackingLogger = _loggerFactory.CreateLogger<TimeTrackingService>();
        _timeTrackingService = new TimeTrackingService(clickUpApi, timeTrackingLogger);
        
        var timeTrackingViewModel = new TimeTrackingViewModel(_timeTrackingService);
        var timeTrackingView = new TimeTrackingView
        {
            DataContext = timeTrackingViewModel
        };
        
        _overlayHost.RegisterToolControl(timeTrackingView);
        
        _overlayHost.Show();

        _logger.LogInformation("Overlay window shown");
    }

    public void Shutdown()
    {
        _timeTrackingService?.Dispose();
        _overlayHost?.Shutdown();
        _logger.LogInformation("Application shutting down");
    }
}

