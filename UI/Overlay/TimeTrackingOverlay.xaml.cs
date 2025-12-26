using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace ClickUpDesktopPowerTools.UI.Overlay;

/// <summary>
/// Time tracking overlay window that displays task name and elapsed time.
/// Positioned at the right edge of the screen, fades content on hover.
/// </summary>
public partial class TimeTrackingOverlay : Window
{
    // P/Invoke declarations for window styles
    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    // P/Invoke for cursor position
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    private struct ContentCardRegion
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    // Track mouse state to prevent flickering
    private bool _isContentHidden = false;
    private bool _wasHovering = false;
    private DispatcherTimer? _mouseCheckTimer;
    private ContentCardRegion _contentCardRegion;

    // Events for context menu actions
    public event EventHandler? StopTimerRequested;
    public event EventHandler? OpenTaskRequested;

    public TimeTrackingOverlay()
    {
        InitializeComponent();
        SourceInitialized += OnSourceInitialized;
        Loaded += OnLoaded;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        // Apply extended window styles to prevent Alt+Tab visibility and focus stealing
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd != IntPtr.Zero)
        {
            var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            extendedStyle |= WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle);
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        PositionWindow();
        CalculateContentCardRegion();
        
        // Start timer to check mouse state using Win32 cursor position (avoids event bubbling issues)
        _mouseCheckTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _mouseCheckTimer.Tick += MouseCheckTimer_Tick;
        _mouseCheckTimer.Start();
    }

    /// <summary>
    /// Calculates the screen coordinates of the content card region for hover detection.
    /// </summary>
    private void CalculateContentCardRegion()
    {
        // Get the content card's position relative to the window
        var contentCardPoint = ContentCard.PointToScreen(new System.Windows.Point(0, 0));
        
        // Get the content card's size
        var contentCardWidth = ContentCard.ActualWidth;
        var contentCardHeight = ContentCard.ActualHeight;
        
        // Store region bounds in screen coordinates
        _contentCardRegion = new ContentCardRegion
        {
            Left = (int)contentCardPoint.X,
            Top = (int)contentCardPoint.Y,
            Right = (int)(contentCardPoint.X + contentCardWidth),
            Bottom = (int)(contentCardPoint.Y + contentCardHeight)
        };
    }

    protected override void OnClosed(EventArgs e)
    {
        _mouseCheckTimer?.Stop();
        _mouseCheckTimer = null;
        base.OnClosed(e);
    }

    /// <summary>
    /// Positions the window at the right edge of the screen, vertically centered in the work area.
    /// </summary>
    private void PositionWindow()
    {
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - Width;
        Top = workArea.Top + (workArea.Height - ActualHeight) / 2;
    }

    /// <summary>
    /// Updates the displayed task name and time text.
    /// </summary>
    public void UpdateContent(string taskName, string timeText)
    {
        TaskNameText.Text = taskName;
        TimeText.Text = timeText;
        
        // Recalculate region if content size might have changed
        if (IsLoaded)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                CalculateContentCardRegion();
            }), DispatcherPriority.Loaded);
        }
    }

    private void MouseCheckTimer_Tick(object? sender, EventArgs e)
    {
        // Use Win32 API to get cursor position directly (avoids WPF event bubbling issues)
        // This is the same approach used in the old codebase - checking screen coordinates
        if (!GetCursorPos(out var cursorPos))
            return;
        
        // Check if cursor is within the content card region using screen coordinates
        bool isHovering = cursorPos.X >= _contentCardRegion.Left &&
                         cursorPos.X <= _contentCardRegion.Right &&
                         cursorPos.Y >= _contentCardRegion.Top &&
                         cursorPos.Y <= _contentCardRegion.Bottom;
        
        // Only animate when hover state actually changes (prevents flickering)
        // This is the key fix - we compare current state with previous state
        if (isHovering != _wasHovering)
        {
            _wasHovering = isHovering;
            
            if (isHovering && !_isContentHidden)
            {
                // Mouse entered - fade out
                _isContentHidden = true;
                var fadeOut = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(150));
                ContentCard.BeginAnimation(OpacityProperty, fadeOut);
            }
            else if (!isHovering && _isContentHidden)
            {
                // Mouse left - fade in
                _isContentHidden = false;
                var fadeIn = new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(150));
                ContentCard.BeginAnimation(OpacityProperty, fadeIn);
            }
        }
    }

    // Keep these handlers for immediate response, but they're now secondary to the timer
    private void ContentCard_MouseEnter(object sender, MouseEventArgs e)
    {
        // Timer will handle this, but this provides immediate feedback
    }

    private void ContentCard_MouseLeave(object sender, MouseEventArgs e)
    {
        // Timer will handle this
    }

    private void Handle_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        // Context menu is handled by WPF's built-in ContextMenu property
        // This handler is here for potential future use
    }

    private void StopTimer_Click(object sender, RoutedEventArgs e)
    {
        StopTimerRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OpenTask_Click(object sender, RoutedEventArgs e)
    {
        OpenTaskRequested?.Invoke(this, EventArgs.Empty);
    }
}

