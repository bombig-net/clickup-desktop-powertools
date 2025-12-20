using System.Windows;
using System.Windows.Controls;

namespace ClickUpDesktopPowerTools.UI.Overlay;

public partial class OverlayWindow : Window
{
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
}

