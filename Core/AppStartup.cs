using Microsoft.Extensions.Logging;
using ClickUpDesktopPowerTools.Core;
using ClickUpDesktopPowerTools.Tools.TimeTracking;

namespace ClickUpDesktopPowerTools.Core;

public class AppStartup
{
    private readonly ILogger<AppStartup> _logger;
    private OverlayHost? _overlayHost;
    private TimeTrackingService? _timeTrackingService;

    public AppStartup(ILogger<AppStartup> logger)
    {
        _logger = logger;
    }

    public void Initialize()
    {
        _logger.LogInformation("Initializing ClickUp Desktop PowerTools");

        _overlayHost = new OverlayHost();
        
        // Create token storage and API
        var tokenStorage = new TokenStorage();
        var clickUpApi = new ClickUpApi(tokenStorage);
        
        // Create TimeTracking tool (service loads team ID from settings and starts polling automatically)
        _timeTrackingService = new TimeTrackingService(clickUpApi);
        
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

