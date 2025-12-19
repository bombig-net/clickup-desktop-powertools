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

    private static double Clamp(double value, double min, double max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    public static Rect GetOverlayPosition()
    {
        // Get taskbar window
        IntPtr taskbarHandle = FindWindow("Shell_TrayWnd", null);
        if (taskbarHandle == IntPtr.Zero)
        {
            // Fallback: use work area
            return GetPositionFromWorkArea();
        }

        // Get taskbar rectangle
        if (GetWindowRect(taskbarHandle, out RECT taskbarRect))
        {
            var dpi = GetDpiForWindow(taskbarHandle);

            var taskbarLeft = PxToDip(taskbarRect.Left, dpi);
            var taskbarTop = PxToDip(taskbarRect.Top, dpi);
            var taskbarRight = PxToDip(taskbarRect.Right, dpi);
            var taskbarBottom = PxToDip(taskbarRect.Bottom, dpi);

            var virtualLeftPx = GetSystemMetrics(SM_XVIRTUALSCREEN);
            var virtualTopPx = GetSystemMetrics(SM_YVIRTUALSCREEN);
            var virtualWidthPx = GetSystemMetrics(SM_CXVIRTUALSCREEN);
            var virtualHeightPx = GetSystemMetrics(SM_CYVIRTUALSCREEN);

            var virtualLeft = PxToDip(virtualLeftPx, dpi);
            var virtualTop = PxToDip(virtualTopPx, dpi);
            var virtualWidth = PxToDip(virtualWidthPx, dpi);
            var virtualHeight = PxToDip(virtualHeightPx, dpi);
            var virtualRight = virtualLeft + virtualWidth;
            var virtualBottom = virtualTop + virtualHeight;

            // Try to use the monitor containing the taskbar for sizing.
            var taskbarPointPx = new POINT
            {
                X = taskbarRect.Left + 1,
                Y = taskbarRect.Top + 1
            };
            var monitorHandle = MonitorFromPoint(taskbarPointPx, MONITOR_DEFAULTTOPRIMARY);
            var monitorInfo = new MONITORINFO { Size = Marshal.SizeOf<MONITORINFO>() };

            double monitorLeft = virtualLeft;
            double monitorTop = virtualTop;
            double monitorRight = virtualRight;
            double monitorBottom = virtualBottom;

            if (monitorHandle != IntPtr.Zero && GetMonitorInfo(monitorHandle, ref monitorInfo))
            {
                monitorLeft = PxToDip(monitorInfo.Monitor.Left, dpi);
                monitorTop = PxToDip(monitorInfo.Monitor.Top, dpi);
                monitorRight = PxToDip(monitorInfo.Monitor.Right, dpi);
                monitorBottom = PxToDip(monitorInfo.Monitor.Bottom, dpi);
            }

            double overlayX = 0;
            double overlayY = 0;
            double overlayHeight = 40; // Default height

            var taskbarWidth = taskbarRight - taskbarLeft;
            var taskbarHeight = taskbarBottom - taskbarTop;
            var monitorWidth = monitorRight - monitorLeft;

            // Horizontal taskbar (top/bottom): span the monitor width.
            if (taskbarWidth >= taskbarHeight)
            {
                overlayX = monitorLeft;
                var overlayWidth = monitorWidth;

                // Bottom if the taskbar is in the lower half of its monitor.
                var monitorMidY = monitorTop + ((monitorBottom - monitorTop) / 2.0);
                if (taskbarTop > monitorMidY)
                {
                    overlayY = taskbarTop - overlayHeight;
                }
                else
                {
                    // Taskbar at top
                    overlayY = taskbarBottom;
                }

                // Clamp to virtual screen bounds.
                overlayWidth = Math.Min(overlayWidth, virtualWidth);
                overlayX = Clamp(overlayX, virtualLeft, virtualRight - overlayWidth);
                overlayY = Clamp(overlayY, virtualTop, virtualBottom - overlayHeight);

                return new Rect(overlayX, overlayY, overlayWidth, overlayHeight);
            }

            // Vertical taskbar (left/right): keep overlay in the content area at the bottom.
            // This keeps behavior predictable without implementing advanced shell edge cases.
            if (taskbarLeft > (monitorLeft + (monitorWidth / 2.0)))
            {
                // Taskbar at right
                overlayX = monitorLeft;
                var overlayWidth = taskbarLeft - monitorLeft;
                overlayY = monitorBottom - overlayHeight;

                overlayWidth = Math.Min(overlayWidth, virtualWidth);
                overlayX = Clamp(overlayX, virtualLeft, virtualRight - overlayWidth);
                overlayY = Clamp(overlayY, virtualTop, virtualBottom - overlayHeight);

                return new Rect(overlayX, overlayY, overlayWidth, overlayHeight);
            }
            else
            {
                // Taskbar at left
                overlayX = taskbarRight;
                var overlayWidth = monitorRight - taskbarRight;
                overlayY = monitorBottom - overlayHeight;

                overlayWidth = Math.Min(overlayWidth, virtualWidth);
                overlayX = Clamp(overlayX, virtualLeft, virtualRight - overlayWidth);
                overlayY = Clamp(overlayY, virtualTop, virtualBottom - overlayHeight);

                return new Rect(overlayX, overlayY, overlayWidth, overlayHeight);
            }

            // Unreachable.
        }

        return GetPositionFromWorkArea();
    }

    private static Rect GetPositionFromWorkArea()
    {
        // Conservative fallback: place at bottom of the virtual screen.
        var virtualLeftPx = GetSystemMetrics(SM_XVIRTUALSCREEN);
        var virtualTopPx = GetSystemMetrics(SM_YVIRTUALSCREEN);
        var virtualWidthPx = GetSystemMetrics(SM_CXVIRTUALSCREEN);
        var virtualHeightPx = GetSystemMetrics(SM_CYVIRTUALSCREEN);

        const double overlayHeight = 40;
        const uint dpi = 96;
        var virtualLeft = PxToDip(virtualLeftPx, dpi);
        var virtualTop = PxToDip(virtualTopPx, dpi);
        var virtualWidth = PxToDip(virtualWidthPx, dpi);
        var virtualHeight = PxToDip(virtualHeightPx, dpi);

        return new Rect(virtualLeft, virtualTop + (virtualHeight - overlayHeight), virtualWidth, overlayHeight);
    }
}

