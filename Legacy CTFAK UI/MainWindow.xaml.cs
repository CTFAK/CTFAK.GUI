using System.Windows;

namespace Legacy_CTFAK_UI
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            //Sets the scaling properly so everything looks right.
            Width += 16;
            Height += 19;
        }

        //I've had issues with memory leaking with Anaconda's GUI so I had to add this.
        private void WindowClosing(object sender, System.EventArgs e)
        {
            Application.Current.Shutdown();
        }

        //All the tabs
        int tab = 0;
        private void closeTabs() {MainCanvas.Visibility = Visibility.Hidden; MFADumpCanvas.Visibility = Visibility.Hidden; PackDumpCanvas.Visibility = Visibility.Hidden; ObjectsCanvas.Visibility = Visibility.Hidden; SoundsCanvas.Visibility = Visibility.Hidden; PluginsCanvas.Visibility = Visibility.Hidden; SettingsCanvas.Visibility = Visibility.Hidden;}
        private void MainTabButton_Click(object sender, RoutedEventArgs e)      {if (tab != 0) {tab = 0; closeTabs(); MainCanvas.Visibility      = Visibility.Visible;}}
        private void MFADumpTabButton_Click(object sender, RoutedEventArgs e)   {if (tab != 1) {tab = 1; closeTabs(); MFADumpCanvas.Visibility   = Visibility.Visible;}}
        private void PackDataTabButton_Click(object sender, RoutedEventArgs e)  {if (tab != 2) {tab = 2; closeTabs(); PackDumpCanvas.Visibility  = Visibility.Visible;}}
        private void ObjectsTabButton_Click(object sender, RoutedEventArgs e)   {if (tab != 3) {tab = 3; closeTabs(); ObjectsCanvas.Visibility   = Visibility.Visible;}}
        private void SoundsTabButton_Click(object sender, RoutedEventArgs e)    {if (tab != 4) {tab = 4; closeTabs(); SoundsCanvas.Visibility    = Visibility.Visible;}}
        private void PluginsTabButton_Click(object sender, RoutedEventArgs e)   {if (tab != 5) {tab = 5; closeTabs(); PluginsCanvas.Visibility   = Visibility.Visible;}}
        private void SettingsTabButton_Click(object sender, RoutedEventArgs e)  {if (tab != 6) {tab = 6; closeTabs(); SettingsCanvas.Visibility  = Visibility.Visible;}}
    }
}
