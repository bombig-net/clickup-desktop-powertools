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

    public void Show()
    {
        if (_overlayWindow == null)
        {
            _overlayWindow = new OverlayWindow();
            Reposition();
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

    public void RegisterToolControl(UIElement control)
    {
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

