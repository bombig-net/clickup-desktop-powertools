using System;
using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.Win32;

namespace ClickUpDesktopPowerTools.Core;

public class WindowPositioning
{
    public static event EventHandler? DisplaySettingsChanged;

    static WindowPositioning()
    {
        SystemEvents.DisplaySettingsChanged += (sender, e) =>
        {
            DisplaySettingsChanged?.Invoke(null, EventArgs.Empty);
        };

        // Taskbar geometry can change without a display mode change (auto-hide, move edge,
        // DPI/theme-related shell adjustments). Treat these as reposition triggers too.
        SystemEvents.UserPreferenceChanged += (sender, e) =>
        {
            DisplaySettingsChanged?.Invoke(null, EventArgs.Empty);
        };
    }

    [DllImport("user32.dll")]
    private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref RECT pvParam, uint fWinIni);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    private const uint SPI_GETWORKAREA = 0x0030;
    private const uint MONITOR_DEFAULTTOPRIMARY = 0x00000001;

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    private const int SM_XVIRTUALSCREEN = 76;
    private const int SM_YVIRTUALSCREEN = 77;
    private const int SM_CXVIRTUALSCREEN = 78;
    private const int SM_CYVIRTUALSCREEN = 79;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public int Size;
        public RECT Monitor;
        public RECT WorkArea;
        public uint Flags;
    }

    private static double PxToDip(int pixels, uint dpi)
    {
        if (dpi == 0)
        {
            dpi = 96;
        }

        return pixels * 96.0 / dpi;
    }

    private static int DipToPx(double dips, uint dpi)
    {
        if (dpi == 0)
        {
            dpi = 96;
        }

        return (int)Math.Round(dips * dpi / 96.0);
    }

    private static int Clamp(int value, int min, int max)
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

    public static bool TryGetTaskbarRectDip(out Rect taskbarRectDip, out bool isHorizontalTaskbar)
    {
        taskbarRectDip = Rect.Empty;
        isHorizontalTaskbar = true;

        IntPtr taskbarHandle = FindWindow("Shell_TrayWnd", null);
        if (taskbarHandle == IntPtr.Zero)
        {
            return false;
        }

        if (!GetWindowRect(taskbarHandle, out RECT taskbarRect))
        {
            return false;
        }

        var dpi = GetDpiForWindow(taskbarHandle);

        var taskbarWidthPx = taskbarRect.Right - taskbarRect.Left;
        var taskbarHeightPx = taskbarRect.Bottom - taskbarRect.Top;
        isHorizontalTaskbar = taskbarWidthPx >= taskbarHeightPx;

        var leftDip = PxToDip(taskbarRect.Left, dpi);
        var topDip = PxToDip(taskbarRect.Top, dpi);
        var widthDip = PxToDip(taskbarWidthPx, dpi);
        var heightDip = PxToDip(taskbarHeightPx, dpi);

        taskbarRectDip = new Rect(leftDip, topDip, widthDip, heightDip);
        return !taskbarRectDip.IsEmpty;
    }

    public static Rect GetOverlayPosition(Size overlayDesiredSizeDip, OverlayPlacementSettings placementSettings)
    {
        if (overlayDesiredSizeDip.Width <= 0 || overlayDesiredSizeDip.Height <= 0)
        {
            return Rect.Empty;
        }

        // Get taskbar window
        IntPtr taskbarHandle = FindWindow("Shell_TrayWnd", null);
        if (taskbarHandle == IntPtr.Zero)
        {
            // If the taskbar cannot be located, we cannot safely position the overlay
            // without risking consuming usable screen space (fullscreen/maximized clipping).
            // Return an empty rect so the overlay effectively does not render.
            return Rect.Empty;
        }

        // Get taskbar rectangle
        if (GetWindowRect(taskbarHandle, out RECT taskbarRect))
        {
            var dpi = GetDpiForWindow(taskbarHandle);

            // Determine which monitor the taskbar is on (for top vs bottom detection).
            var taskbarPointPx = new POINT { X = taskbarRect.Left + 1, Y = taskbarRect.Top + 1 };
            var monitorHandle = MonitorFromPoint(taskbarPointPx, MONITOR_DEFAULTTOPRIMARY);
            var monitorInfo = new MONITORINFO { Size = Marshal.SizeOf<MONITORINFO>() };
            var haveMonitorInfo = monitorHandle != IntPtr.Zero && GetMonitorInfo(monitorHandle, ref monitorInfo);

            var taskbarWidthPx = taskbarRect.Right - taskbarRect.Left;
            var taskbarHeightPx = taskbarRect.Bottom - taskbarRect.Top;
            var isHorizontalTaskbar = taskbarWidthPx >= taskbarHeightPx;

            var desiredOverlayWidthPx = Math.Max(1, DipToPx(overlayDesiredSizeDip.Width, dpi));
            var desiredOverlayHeightPx = Math.Max(1, DipToPx(overlayDesiredSizeDip.Height, dpi));

            // Size clamping rules:
            // - Horizontal taskbar: width <= taskbarWidth, height <= taskbarHeight
            // - Vertical taskbar: width <= taskbarWidth (thickness), height <= taskbarHeight
            var overlayWidthPx = Math.Min(desiredOverlayWidthPx, taskbarWidthPx);
            var overlayHeightPx = Math.Min(desiredOverlayHeightPx, taskbarHeightPx);

            var offsetPx = DipToPx(placementSettings.OverlayOffset, dpi);

            var overlayLeftPx = 0;
            var overlayTopPx = 0;

            if (isHorizontalTaskbar)
            {
                int xCandidatePx;
                switch (placementSettings.OverlayDock)
                {
                    case OverlayDock.Left:
                        xCandidatePx = taskbarRect.Left + offsetPx;
                        break;
                    case OverlayDock.Center:
                        xCandidatePx = taskbarRect.Left + ((taskbarWidthPx - overlayWidthPx) / 2) + offsetPx;
                        break;
                    case OverlayDock.Right:
                        xCandidatePx = taskbarRect.Right - overlayWidthPx - offsetPx;
                        break;
                    default:
                        xCandidatePx = taskbarRect.Right - overlayWidthPx - offsetPx;
                        break;
                }

                // Center overlay vertically within taskbar
                var yCandidatePx = taskbarRect.Top + ((taskbarHeightPx - overlayHeightPx) / 2);

                overlayLeftPx = Clamp(xCandidatePx, taskbarRect.Left, taskbarRect.Right - overlayWidthPx);
                overlayTopPx = Clamp(yCandidatePx, taskbarRect.Top, taskbarRect.Bottom - overlayHeightPx);
            }
            else
            {
                // Vertical taskbar (left/right): dock maps Left|Center|Right to Top|Center|Bottom (intentional).
                int yCandidatePx;
                switch (placementSettings.OverlayDock)
                {
                    case OverlayDock.Left:
                        yCandidatePx = taskbarRect.Top + offsetPx;
                        break;
                    case OverlayDock.Center:
                        yCandidatePx = taskbarRect.Top + ((taskbarHeightPx - overlayHeightPx) / 2) + offsetPx;
                        break;
                    case OverlayDock.Right:
                        yCandidatePx = taskbarRect.Bottom - overlayHeightPx - offsetPx;
                        break;
                    default:
                        yCandidatePx = taskbarRect.Bottom - overlayHeightPx - offsetPx;
                        break;
                }

                var isRightTaskbar = false;
                if (haveMonitorInfo)
                {
                    var monitorMidXpx = monitorInfo.Monitor.Left + ((monitorInfo.Monitor.Right - monitorInfo.Monitor.Left) / 2);
                    isRightTaskbar = taskbarRect.Left > monitorMidXpx;
                }

                var xCandidatePx = isRightTaskbar
                    ? taskbarRect.Right - overlayWidthPx
                    : taskbarRect.Left;

                overlayLeftPx = Clamp(xCandidatePx, taskbarRect.Left, taskbarRect.Right - overlayWidthPx);
                overlayTopPx = Clamp(yCandidatePx, taskbarRect.Top, taskbarRect.Bottom - overlayHeightPx);
            }

            return new Rect(
                PxToDip(overlayLeftPx, dpi),
                PxToDip(overlayTopPx, dpi),
                PxToDip(overlayWidthPx, dpi),
                PxToDip(overlayHeightPx, dpi));
        }

        // If the taskbar rect cannot be read, do not guess.
        return Rect.Empty;
    }
}

