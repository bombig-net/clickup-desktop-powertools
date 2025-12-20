using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;

namespace ClickUpDesktopPowerTools.UI.Overlay;

public enum OverlayRawMousePhase
{
    Down,
    Move,
    Up
}

public partial class OverlayWindow : Window
{
    // Win32 P/Invoke declarations
    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
        int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    private static extern IntPtr SetWinEventHook(
        uint eventMin, uint eventMax,
        IntPtr hmodWinEventProc,
        WinEventDelegate lpfnWinEventProc,
        uint idProcess, uint idThread,
        uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

    private delegate void WinEventDelegate(
        IntPtr hWinEventHook, uint eventType,
        IntPtr hwnd, int idObject, int idChild,
        uint dwEventThread, uint dwmsEventTime);

    // Win32 constants
    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    private const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
    private const uint WINEVENT_OUTOFCONTEXT = 0x0000;
    private const uint WINEVENT_SKIPOWNPROCESS = 0x0002;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;

    // Fields
    private bool _isCtrlDragCaptureActive;
    private bool _isReassertingTopmost;
    private HwndSource? _hwndSource;
    private IntPtr _winEventHook;
    private WinEventDelegate? _winEventDelegate;

    public Action<OverlayRawMousePhase, Point>? RawMouse;

    public OverlayWindow()
    {
        InitializeComponent();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _hwndSource = (HwndSource)PresentationSource.FromVisual(this);

        // Store delegate in field to prevent garbage collection
        _winEventDelegate = OnForegroundChanged;
        _winEventHook = SetWinEventHook(
            EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND,
            IntPtr.Zero,
            _winEventDelegate,
            0, 0,
            WINEVENT_OUTOFCONTEXT | WINEVENT_SKIPOWNPROCESS);
    }

    protected override void OnClosed(EventArgs e)
    {
        if (_winEventHook != IntPtr.Zero)
        {
            UnhookWinEvent(_winEventHook);
            _winEventHook = IntPtr.Zero;
        }
        base.OnClosed(e);
    }

    private void OnForegroundChanged(
        IntPtr hWinEventHook, uint eventType,
        IntPtr hwnd, int idObject, int idChild,
        uint dwEventThread, uint dwmsEventTime)
    {
        if (!_isReassertingTopmost && IsVisible)
        {
            ReassertTopmost();
        }
    }

    private void ReassertTopmost()
    {
        if (_hwndSource == null)
        {
            return;
        }

        _isReassertingTopmost = true;
        try
        {
            SetWindowPos(
                _hwndSource.Handle,
                HWND_TOPMOST,
                0, 0, 0, 0,
                SWP_NOACTIVATE | SWP_NOMOVE | SWP_NOSIZE);
        }
        finally
        {
            _isReassertingTopmost = false;
        }
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

