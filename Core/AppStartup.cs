using Microsoft.Extensions.Logging;
using ClickUpDesktopPowerTools.Core;
using ClickUpDesktopPowerTools.Tools.TimeTracking;

namespace ClickUpDesktopPowerTools.Core;

public class AppStartup
{
    private readonly ILogger<AppStartup> _logger;
    private OverlayHost? _overlayHost;

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
        
        // Create TimeTracking tool
        var timeTrackingService = new TimeTrackingService(clickUpApi);
        
        // Load current time entry asynchronously
        _ = Task.Run(async () =>
        {
            await timeTrackingService.LoadCurrentTimeEntry();
        });
        
        var timeTrackingViewModel = new TimeTrackingViewModel(timeTrackingService);
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
        _overlayHost?.Hide();
        _logger.LogInformation("Application shutting down");
    }
}

