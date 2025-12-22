using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace ClickUpDesktopPowerTools.UI.Control;

public partial class ControlWindow : Window
{
    private bool _allowClose = false;
    private bool _isInitialized = false;

    public ControlWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;

        try
        {
            // Set UserDataFolder before initialization to avoid default location issues
            WebView.CreationProperties = new CoreWebView2CreationProperties
            {
                UserDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "ClickUpDesktopPowerTools",
                    "WebView2Data")
            };

            await WebView.EnsureCoreWebView2Async();

            // Get the wwwroot folder path (relative to executable)
            var exeDirectory = AppContext.BaseDirectory;
            var wwwrootPath = Path.Combine(exeDirectory, "wwwroot");

            WebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "powertools.local",
                wwwrootPath,
                CoreWebView2HostResourceAccessKind.Allow);

            WebView.CoreWebView2.Navigate("https://powertools.local/index.html");
        }
        catch (Exception ex)
        {
            // WebView2 runtime not installed or initialization failed
            MessageBox.Show(
                $"Failed to initialize WebView2: {ex.Message}\n\nPlease ensure the WebView2 Runtime is installed.",
                "Initialization Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_allowClose)
        {
            e.Cancel = true;
            Hide();
            return;
        }
        // _allowClose is true: let the close proceed (disposes WebView2)
        base.OnClosing(e);
    }

    public void ForceClose()
    {
        _allowClose = true;
        Close();
    }
}

