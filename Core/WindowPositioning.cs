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
    private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref RECT pvParam, uint fWinIni);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    private const uint SPI_GETWORKAREA = 0x0030;
    private const uint MONITOR_DEFAULTTOPRIMARY = 0x00000001;

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
            // Determine taskbar position
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;

            double overlayX = 0;
            double overlayY = 0;
            double overlayWidth = screenWidth;
            double overlayHeight = 40; // Default height

            // Taskbar is typically at bottom
            if (taskbarRect.Top > screenHeight / 2)
            {
                // Taskbar at bottom
                overlayY = taskbarRect.Top - overlayHeight;
            }
            else if (taskbarRect.Left > screenWidth / 2)
            {
                // Taskbar at right
                overlayX = taskbarRect.Left - overlayWidth;
                overlayY = 0;
            }
            else if (taskbarRect.Right < screenWidth / 2)
            {
                // Taskbar at left
                overlayX = taskbarRect.Right;
                overlayY = 0;
            }
            else
            {
                // Taskbar at top
                overlayY = taskbarRect.Bottom;
            }

            return new Rect(overlayX, overlayY, overlayWidth, overlayHeight);
        }

        return GetPositionFromWorkArea();
    }

    private static Rect GetPositionFromWorkArea()
    {
        var screenWidth = SystemParameters.PrimaryScreenWidth;
        var screenHeight = SystemParameters.PrimaryScreenHeight;
        return new Rect(0, screenHeight - 40, screenWidth, 40);
    }
}

