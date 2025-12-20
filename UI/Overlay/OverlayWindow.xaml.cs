using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ClickUpDesktopPowerTools.UI.Overlay;

public enum OverlayRawMousePhase
{
    Down,
    Move,
    Up
}

public partial class OverlayWindow : Window
{
    private bool _isCtrlDragCaptureActive;

    public Action<OverlayRawMousePhase, Point>? RawMouse;

    public OverlayWindow()
    {
        InitializeComponent();
    }

    public FrameworkElement GetContentRoot()
    {
        return ContentRoot;
    }

    private void OnCloseClicked(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    public void AddToolControl(UIElement control)
    {
        ToolContainer.Children.Add(control);
    }

    public void ClearToolControls()
    {
        ToolContainer.Children.Clear();
    }

    private void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control)
        {
            return;
        }

        _isCtrlDragCaptureActive = true;
        CaptureMouse();

        RawMouse?.Invoke(OverlayRawMousePhase.Down, GetMouseScreenPositionDip(e));
        e.Handled = true;
    }

    private void OnPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isCtrlDragCaptureActive)
        {
            return;
        }

        RawMouse?.Invoke(OverlayRawMousePhase.Move, GetMouseScreenPositionDip(e));
        e.Handled = true;
    }

    private void OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isCtrlDragCaptureActive)
        {
            return;
        }

        RawMouse?.Invoke(OverlayRawMousePhase.Up, GetMouseScreenPositionDip(e));

        _isCtrlDragCaptureActive = false;
        ReleaseMouseCapture();

        e.Handled = true;
    }

    private Point GetMouseScreenPositionDip(MouseEventArgs e)
    {
        var mouseInWindowDip = e.GetPosition(this);
        return new Point(Left + mouseInWindowDip.X, Top + mouseInWindowDip.Y);
    }
}

