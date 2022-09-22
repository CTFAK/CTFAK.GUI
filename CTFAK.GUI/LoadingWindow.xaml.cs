using System.Windows;
using System.Windows.Media;

namespace Legacy_CTFAK_UI;

public partial class LoadingWindow : Window
{
    public LoadingWindow()
    {
        InitializeComponent();
        Loader.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(MainWindow.Color));
        LoadingLabel.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(MainWindow.Color));
        LoadingProgressBarOne.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(MainWindow.Color));
    }
}