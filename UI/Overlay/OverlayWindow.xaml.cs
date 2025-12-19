using System.Windows;
using System.Windows.Controls;

namespace ClickUpDesktopPowerTools.UI.Overlay;

public partial class OverlayWindow : Window
{
    public OverlayWindow()
    {
        InitializeComponent();
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

