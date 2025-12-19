using System.Windows;
using ClickUpDesktopPowerTools.Core;
using ClickUpDesktopPowerTools.UI.Overlay;

namespace ClickUpDesktopPowerTools.Core;

public class OverlayHost
{
    private OverlayWindow? _overlayWindow;

    public OverlayHost()
    {
        WindowPositioning.DisplaySettingsChanged += OnDisplaySettingsChanged;
    }

    private void EnsureWindowCreated()
    {
        if (_overlayWindow != null)
        {
            return;
        }

        _overlayWindow = new OverlayWindow();
        Reposition();
    }

    public void Show()
    {
        EnsureWindowCreated();

        // Re-apply position on every show (display metrics can change while hidden).
        Reposition();

        if (_overlayWindow != null && !_overlayWindow.IsVisible)
        {
            _overlayWindow.Show();
        }
    }

    private void Reposition()
    {
        if (_overlayWindow != null)
        {
            var position = WindowPositioning.GetOverlayPosition();
            _overlayWindow.Left = position.Left;
            _overlayWindow.Top = position.Top;
            _overlayWindow.Width = position.Width;
            _overlayWindow.Height = position.Height;
        }
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        Reposition();
    }

    public void Hide()
    {
        _overlayWindow?.Hide();
    }

    public void Shutdown()
    {
        WindowPositioning.DisplaySettingsChanged -= OnDisplaySettingsChanged;

        if (_overlayWindow != null)
        {
            _overlayWindow.Close();
            _overlayWindow = null;
        }
    }

    public void RegisterToolControl(UIElement control)
    {
        EnsureWindowCreated();
        _overlayWindow?.AddToolControl(control);
    }

    public void UnregisterToolControl(UIElement control)
    {
        // Simple implementation - could be enhanced if needed
        if (_overlayWindow != null)
        {
            _overlayWindow.ClearToolControls();
        }
    }
}

