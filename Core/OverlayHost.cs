using System;
using System.Windows;
using ClickUpDesktopPowerTools.UI.Overlay;

namespace ClickUpDesktopPowerTools.Core;

public class OverlayHost
{
    private OverlayWindow? _overlayWindow;
    private readonly OverlayPlacementSettings _placementSettings;

    private bool _isDragging;
    private Rect _dragTaskbarRectDip;
    private bool _dragIsHorizontalTaskbar;
    private double _dragGrabOffsetAxisDip;
    private double _dragOverlayWidthDip;
    private double _dragOverlayHeightDip;
    private double _dragFixedAxisPosDip;

    public OverlayHost()
        : this(() => OverlayPlacementSettings.Load())
    {
    }

    public OverlayHost(Func<OverlayPlacementSettings> getPlacementSettings)
    {
        _placementSettings = getPlacementSettings();
        WindowPositioning.DisplaySettingsChanged += OnDisplaySettingsChanged;
    }

    private void EnsureWindowCreated()
    {
        if (_overlayWindow != null)
        {
            return;
        }

        _overlayWindow = new OverlayWindow();
        _overlayWindow.RawMouse = OnOverlayRawMouse;
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
            var contentRoot = _overlayWindow.GetContentRoot();
            contentRoot.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var desiredSizeDip = contentRoot.DesiredSize;

            var position = WindowPositioning.GetOverlayPosition(desiredSizeDip, _placementSettings);
            _overlayWindow.Left = position.Left;
            _overlayWindow.Top = position.Top;
            _overlayWindow.Width = position.Width;
            _overlayWindow.Height = position.Height;
        }
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        if (_isDragging)
        {
            return;
        }

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
            _overlayWindow.RawMouse = null;
            _overlayWindow.Close();
            _overlayWindow = null;
        }
    }

    public void RegisterToolControl(UIElement control)
    {
        EnsureWindowCreated();
        _overlayWindow?.AddToolControl(control);
        Reposition();
    }

    public void UnregisterToolControl(UIElement control)
    {
        // Simple implementation - could be enhanced if needed
        if (_overlayWindow != null)
        {
            _overlayWindow.ClearToolControls();
            Reposition();
        }
    }

    private void OnOverlayRawMouse(OverlayRawMousePhase phase, Point mouseScreenDip)
    {
        if (_overlayWindow == null)
        {
            return;
        }

        switch (phase)
        {
            case OverlayRawMousePhase.Down:
                BeginDrag(mouseScreenDip);
                break;
            case OverlayRawMousePhase.Move:
                UpdateDrag(mouseScreenDip);
                break;
            case OverlayRawMousePhase.Up:
                EndDrag(mouseScreenDip);
                break;
            default:
                break;
        }
    }

    private void BeginDrag(Point mouseScreenDip)
    {
        if (_overlayWindow == null)
        {
            return;
        }

        if (!WindowPositioning.TryGetTaskbarRectDip(out var taskbarRectDip, out var isHorizontalTaskbar))
        {
            return;
        }

        _dragTaskbarRectDip = taskbarRectDip;
        _dragIsHorizontalTaskbar = isHorizontalTaskbar;

        _dragOverlayWidthDip = _overlayWindow.ActualWidth > 0 ? _overlayWindow.ActualWidth : _overlayWindow.Width;
        _dragOverlayHeightDip = _overlayWindow.ActualHeight > 0 ? _overlayWindow.ActualHeight : _overlayWindow.Height;

        if (_dragOverlayWidthDip <= 0 || _dragOverlayHeightDip <= 0)
        {
            return;
        }

        var mouseAxisDip = _dragIsHorizontalTaskbar ? mouseScreenDip.X : mouseScreenDip.Y;
        var overlayAxisPosDip = _dragIsHorizontalTaskbar ? _overlayWindow.Left : _overlayWindow.Top;

        // Explicit grab-offset math (no jumping):
        // grabOffsetAxis = mouseAxis - overlayAxisPos
        _dragGrabOffsetAxisDip = mouseAxisDip - overlayAxisPosDip;
        _dragFixedAxisPosDip = _dragIsHorizontalTaskbar ? _overlayWindow.Top : _overlayWindow.Left;

        _isDragging = true;
    }

    private void UpdateDrag(Point mouseScreenDip)
    {
        if (_overlayWindow == null || !_isDragging)
        {
            return;
        }

        var mouseAxisDip = _dragIsHorizontalTaskbar ? mouseScreenDip.X : mouseScreenDip.Y;

        // Explicit grab-offset math (no jumping):
        // overlayAxisPos = mouseAxis - grabOffsetAxis
        var overlayAxisPosDip = mouseAxisDip - _dragGrabOffsetAxisDip;

        double leftDip;
        double topDip;

        if (_dragIsHorizontalTaskbar)
        {
            leftDip = overlayAxisPosDip;
            topDip = _dragFixedAxisPosDip;
        }
        else
        {
            leftDip = _dragFixedAxisPosDip;
            topDip = overlayAxisPosDip;
        }

        (leftDip, topDip) = ClampOverlayInsideTaskbar(leftDip, topDip);

        _overlayWindow.Left = leftDip;
        _overlayWindow.Top = topDip;
    }

    private void EndDrag(Point mouseScreenDip)
    {
        if (_overlayWindow == null || !_isDragging)
        {
            return;
        }

        // Apply one last update using the final mouse position.
        UpdateDrag(mouseScreenDip);

        var overlayLeftDip = _overlayWindow.Left;
        var overlayTopDip = _overlayWindow.Top;

        var dock = DetermineDock(_dragTaskbarRectDip, overlayLeftDip, overlayTopDip, _dragOverlayWidthDip, _dragOverlayHeightDip, _dragIsHorizontalTaskbar);
        var offsetDip = CalculateOffsetDip(_dragTaskbarRectDip, overlayLeftDip, overlayTopDip, _dragOverlayWidthDip, _dragOverlayHeightDip, _dragIsHorizontalTaskbar, dock);

        _placementSettings.OverlayDock = dock;
        _placementSettings.OverlayOffset = (int)Math.Round(offsetDip);
        _placementSettings.Save();

        _isDragging = false;

        // Re-apply using the authoritative positioning logic (snap + clamp).
        Reposition();
    }

    private (double leftDip, double topDip) ClampOverlayInsideTaskbar(double leftDip, double topDip)
    {
        var minLeft = _dragTaskbarRectDip.Left;
        var maxLeft = _dragTaskbarRectDip.Right - _dragOverlayWidthDip;
        if (maxLeft < minLeft)
        {
            maxLeft = minLeft;
        }

        var minTop = _dragTaskbarRectDip.Top;
        var maxTop = _dragTaskbarRectDip.Bottom - _dragOverlayHeightDip;
        if (maxTop < minTop)
        {
            maxTop = minTop;
        }

        return (ClampDouble(leftDip, minLeft, maxLeft), ClampDouble(topDip, minTop, maxTop));
    }

    private static double ClampDouble(double value, double min, double max)
    {
        if (value < min)
        {
            return min;
        }
        if (value > max)
        {
            return max;
        }

        return value;
    }

    private static OverlayDock DetermineDock(Rect taskbarRectDip, double overlayLeftDip, double overlayTopDip, double overlayWidthDip, double overlayHeightDip, bool isHorizontalTaskbar)
    {
        var axisMin = isHorizontalTaskbar ? taskbarRectDip.Left : taskbarRectDip.Top;
        var axisMax = isHorizontalTaskbar ? taskbarRectDip.Right : taskbarRectDip.Bottom;
        var axisSize = Math.Max(1.0, axisMax - axisMin);

        var overlayAxisCenter = isHorizontalTaskbar
            ? (overlayLeftDip + (overlayWidthDip / 2.0))
            : (overlayTopDip + (overlayHeightDip / 2.0));

        var third = axisSize / 3.0;
        if (overlayAxisCenter < axisMin + third)
        {
            return OverlayDock.Left;
        }
        if (overlayAxisCenter < axisMin + (2.0 * third))
        {
            return OverlayDock.Center;
        }

        return OverlayDock.Right;
    }

    private static double CalculateOffsetDip(Rect taskbarRectDip, double overlayLeftDip, double overlayTopDip, double overlayWidthDip, double overlayHeightDip, bool isHorizontalTaskbar, OverlayDock dock)
    {
        if (isHorizontalTaskbar)
        {
            var leftAnchor = taskbarRectDip.Left;
            var centerAnchor = taskbarRectDip.Left + ((taskbarRectDip.Width - overlayWidthDip) / 2.0);
            var rightAnchor = taskbarRectDip.Right - overlayWidthDip;

            return dock switch
            {
                // Left: distance from left edge (non-negative)
                OverlayDock.Left => overlayLeftDip - leftAnchor,
                // Center: signed offset (left negative, right positive)
                OverlayDock.Center => overlayLeftDip - centerAnchor,
                // Right: distance from right edge (non-negative)
                OverlayDock.Right => rightAnchor - overlayLeftDip,
                _ => rightAnchor - overlayLeftDip
            };
        }
        else
        {
            // Vertical taskbar: dock maps Left|Center|Right to Top|Center|Bottom (intentional).
            var topAnchor = taskbarRectDip.Top;
            var centerAnchor = taskbarRectDip.Top + ((taskbarRectDip.Height - overlayHeightDip) / 2.0);
            var bottomAnchor = taskbarRectDip.Bottom - overlayHeightDip;

            return dock switch
            {
                // Top: distance from top edge (non-negative)
                OverlayDock.Left => overlayTopDip - topAnchor,
                // Center: signed offset (up negative, down positive)
                OverlayDock.Center => overlayTopDip - centerAnchor,
                // Bottom: distance from bottom edge (non-negative)
                OverlayDock.Right => bottomAnchor - overlayTopDip,
                _ => bottomAnchor - overlayTopDip
            };
        }
    }
}

