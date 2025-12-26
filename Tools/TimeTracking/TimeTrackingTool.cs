using System;
using System.Windows;
using ClickUpDesktopPowerTools.Core;
using ClickUpDesktopPowerTools.UI.Overlay;
using Microsoft.Extensions.Logging;

namespace ClickUpDesktopPowerTools.Tools.TimeTracking;

/// <summary>
/// Time Tracking tool that displays a minimal overlay showing the currently tracked task and elapsed time.
/// The overlay appears on the right edge of the screen only when a timer is running or in test mode.
/// </summary>
public class TimeTrackingTool : IToolLifecycle
{
    private readonly ILogger<TimeTrackingTool> _logger;
    private RuntimeContext? _runtimeContext;
    private TimeTrackingOverlay? _overlay;
    private bool _isEnabled;
    private bool _isTestMode;

    // Placeholder data for testing
    private const string PlaceholderTaskName = "Taskname Lorem ipsum dolor sit";
    private const string PlaceholderTimeText = "Session: 04:27h / Total: 06:26";

    public TimeTrackingTool(ILogger<TimeTrackingTool> logger)
    {
        _logger = logger;
    }

    public void OnEnable()
    {
        _isEnabled = true;
        _logger.LogInformation("Time Tracking tool enabled");
        // Do not show overlay yet - wait for timer detection or test mode
    }

    public void OnDisable()
    {
        _isEnabled = false;
        _isTestMode = false;
        _logger.LogInformation("Time Tracking tool disabled");
        HideOverlay();
    }

    public void OnRuntimeReady(RuntimeContext ctx)
    {
        _runtimeContext = ctx;
        _logger.LogInformation("Runtime ready for Time Tracking tool");
        // Timer detection would be implemented here in the future
        // For now, overlay only shows via test mode
    }

    public void OnRuntimeDisconnected()
    {
        _runtimeContext = null;
        _logger.LogInformation("Runtime disconnected for Time Tracking tool");
        
        // Only hide if not in test mode
        if (!_isTestMode)
        {
            HideOverlay();
        }
    }

    /// <summary>
    /// Shows the overlay with placeholder data for testing purposes.
    /// Called from ControlWindow when user clicks the test button.
    /// </summary>
    public void ShowOverlayForTesting()
    {
        if (!_isEnabled)
        {
            _logger.LogWarning("Cannot show test overlay - tool is not enabled");
            return;
        }

        _isTestMode = true;
        _logger.LogInformation("Showing overlay in test mode");
        ShowOverlay(PlaceholderTaskName, PlaceholderTimeText);
    }

    /// <summary>
    /// Hides the overlay and clears test mode.
    /// Can be called from context menu "Stop Timer" action.
    /// </summary>
    public void HideOverlayAndClearTestMode()
    {
        _isTestMode = false;
        HideOverlay();
        _logger.LogInformation("Overlay hidden and test mode cleared");
    }

    private void ShowOverlay(string taskName, string timeText)
    {
        // WPF windows must be created/manipulated on UI thread
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (_overlay == null)
            {
                _overlay = new TimeTrackingOverlay();
                _overlay.StopTimerRequested += OnStopTimerRequested;
                _overlay.OpenTaskRequested += OnOpenTaskRequested;
            }

            _overlay.UpdateContent(taskName, timeText);
            _overlay.Show();
            _logger.LogDebug("Overlay shown with task: {TaskName}", taskName);
        });
    }

    private void HideOverlay()
    {
        // WPF windows must be manipulated on UI thread
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (_overlay != null)
            {
                _overlay.Hide();
                _logger.LogDebug("Overlay hidden");
            }
        });
    }

    private void OnStopTimerRequested(object? sender, EventArgs e)
    {
        _logger.LogInformation("Stop Timer requested from overlay context menu");
        // For now, just hide the overlay and clear test mode
        // In the future, this would actually stop the timer via RuntimeContext
        HideOverlayAndClearTestMode();
    }

    private void OnOpenTaskRequested(object? sender, EventArgs e)
    {
        _logger.LogInformation("Open Task requested from overlay context menu");
        // No-op for now - would navigate to task in ClickUp in the future
    }
}

