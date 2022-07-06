using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using CTFAK;
using CTFAK.CCN;
using CTFAK.CCN.Chunks.Banks;
using CTFAK.FileReaders;
using CTFAK.Utils;
using Microsoft.Win32;

namespace Legacy_CTFAK_UI
{
    public partial class MainWindow : Window
    {
        public LoadingWindow loadingWindow;

        public void UpdateProgress(double incrementBy)
        {
            this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() 
            {       
               loadingWindow.LoadingProgressBarOne.Value+=incrementBy; // Do all the ui thread updates here
            }));
        }
        public MainWindow()
        {
            InitializeComponent();
            Core.Init();
            ImageBank.OnImageLoaded += (current, all) =>
            {
                UpdateProgress(1.0 / all);
            };
            GameData.OnChunkLoaded += (current, all) =>
            {
                //UpdateProgress(0.05);
            };
            GameData.OnFrameLoaded += (current, all) =>
            {
                //UpdateProgress(1.0 / all);
            };
            SoundBank.OnSoundLoaded += (current, all) =>
            {
                //UpdateProgress(1.0 / all);
            };
            

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

        void AfterGUILoaded()
        {
            OpenDumpFolderButton.Visibility = Visibility.Visible;
            MFAInfoTextBlock.Visibility = Visibility.Visible;
            DumpImagesButton.Visibility = Visibility.Visible;
            DumpSoundsButton.Visibility = Visibility.Visible;
            DumpMusicButton.Visibility = Visibility.Visible;
            ItemInfoTextBlock.Visibility = Visibility.Visible;
            MFATreeView.Visibility = Visibility.Visible;
            var game = currentReader.getGameData();
            MFAInfoTextBlock.Content =
                $"Title: {game.name ?? ""}\n" +
                $"Copyright: {game.copyright ?? ""}\n" +
                $"Product Version: to be filled\n" +
                $"Build: {Settings.Build}\n" +
                $"Runtime Version: to be filled\n" +
                $"Number of Images: {game?.images?.Items.Count ?? 0}\n" +
                $"Number of Sounds: {game?.Sounds?.Items.Count ?? 0}\n" +
                $"Number of Music: {game?.Music?.Items.Count ?? 0}\n" +
                $"Unique FrameItems: {game?.frameitems?.Count ?? 0}\n" +
                $"Frame Count: {game.frames.Count}\n";




        }

        public static IFileReader currentReader;
        private void SelectFileButton_OnClick(object sender, RoutedEventArgs e)
        {
            var fileSelector = new OpenFileDialog();
            fileSelector.Title = "Select game";
            fileSelector.CheckFileExists = true;
            fileSelector.CheckPathExists = true;
            fileSelector.Filter = @"Fusion executable|*.exe|Fusion CCN|*.ccn|Fusion Android game|*.apk";
            if (fileSelector.ShowDialog().Value)
            {
                currentReader = new ExeFileReader();
                Core.parameters = "";
                Core.path = fileSelector.FileName;

                loadingWindow = new LoadingWindow();
                loadingWindow.Owner = this;
                loadingWindow.Show();
                var backgroundWorker = new BackgroundWorker();
                backgroundWorker.DoWork += (o,e) =>
                {
                    currentReader.LoadGame(fileSelector.FileName);
                };
                backgroundWorker.RunWorkerCompleted += (o, e) =>
                {
                    loadingWindow.Close();
                    AfterGUILoaded();
                };
                
                backgroundWorker.RunWorkerAsync();
            }
        }
    }
}
