using System;
using System.Windows;
using H.NotifyIcon;
using Microsoft.Extensions.Logging;
using ClickUpDesktopPowerTools.UI.Control;

namespace ClickUpDesktopPowerTools.Core;

public class TrayHost : IDisposable
{
    private readonly TokenStorage _tokenStorage;
    private readonly ClickUpApi _clickUpApi;
    private readonly CoreState _coreState;
    private readonly ClickUpRuntime _clickUpRuntime;
    private readonly ILoggerFactory _loggerFactory;

    private TaskbarIcon? _taskbarIcon;
    private ControlWindow? _controlWindow;
    private bool _disposed;

    public TrayHost(TokenStorage tokenStorage, ClickUpApi clickUpApi, CoreState coreState, ClickUpRuntime clickUpRuntime, ILoggerFactory loggerFactory)
    {
        _tokenStorage = tokenStorage;
        _clickUpApi = clickUpApi;
        _coreState = coreState;
        _clickUpRuntime = clickUpRuntime;
        _loggerFactory = loggerFactory;
    }

    public void Initialize()
    {
        var resourceStream = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/tray-icon.ico"));
        var icon = new System.Drawing.Icon(resourceStream!.Stream);

        _taskbarIcon = new TaskbarIcon
        {
            Icon = icon,
            ToolTipText = "ClickUp Desktop PowerTools",
            ContextMenu = CreateContextMenu()
        };

        _taskbarIcon.ForceCreate();
        _taskbarIcon.TrayLeftMouseDown += OnTrayLeftMouseDown;
    }

    private void OnTrayLeftMouseDown(object sender, RoutedEventArgs e)
    {
        ShowControlWindow();
    }

    private System.Windows.Controls.ContextMenu CreateContextMenu()
    {
        var menu = new System.Windows.Controls.ContextMenu();

        var settingsItem = new System.Windows.Controls.MenuItem { Header = "Settings..." };
        settingsItem.Click += OnSettingsClicked;
        menu.Items.Add(settingsItem);

        menu.Items.Add(new System.Windows.Controls.Separator());

        var exitItem = new System.Windows.Controls.MenuItem { Header = "Exit" };
        exitItem.Click += OnExitClicked;
        menu.Items.Add(exitItem);

        return menu;
    }

    private void OnSettingsClicked(object sender, RoutedEventArgs e)
    {
        ShowControlWindow();
    }

    private void ShowControlWindow()
    {
        if (_controlWindow == null)
        {
            _controlWindow = new ControlWindow(_tokenStorage, _clickUpApi, _coreState, _clickUpRuntime, _loggerFactory);
        }

        if (_controlWindow.IsVisible)
        {
            _controlWindow.Activate();
        }
        else
        {
            _controlWindow.Show();
        }
    }

    private void OnExitClicked(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_controlWindow != null)
        {
            _controlWindow.ForceClose();
            _controlWindow = null;
        }

        if (_taskbarIcon != null)
        {
            _taskbarIcon.Dispose();
            _taskbarIcon = null;
        }
    }
}
