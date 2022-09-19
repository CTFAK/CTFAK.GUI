using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using CTFAK;
using CTFAK.CCN;
using CTFAK.CCN.Chunks.Banks;
using CTFAK.CCN.Chunks.Frame;
using CTFAK.CCN.Chunks.Objects;
using CTFAK.EXE;
using CTFAK.FileReaders;
using CTFAK.Tools;
using CTFAK.Utils;
using Microsoft.Win32;
using Action = System.Action;

namespace Legacy_CTFAK_UI
{
    public partial class MainWindow : Window
    {
        public LoadingWindow loadingWindow;
        public IFusionTool imageDumper;
        public IFusionTool soundDumper;
        public SoundPlayer CurrentPlayingSound;
        List<int> loadMax = new List<int>() {0,0,0,0};

        public void UpdateProgress(double incrementBy)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() 
            {
                loadingWindow.LoadingProgressBarOne.Maximum = loadMax[0] + loadMax[1] + loadMax[2] + loadMax[3];
                loadingWindow.LoadingProgressBarOne.Value+=incrementBy;
            }));
        }

        public void UpdateImageProgress(float Progress, float Max)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate()
            {
                DumpImagesProgressBar.Maximum = Max;
                DumpImagesProgressBar.Value = Progress;
                if (Progress == Max)
                    DumpImagesProgressBar.Visibility = Visibility.Hidden;
            }));
        }

        public void UpdateSoundProgress(float Progress, float Max)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate ()
            {
                DumpSoundsProgressBar.Maximum = Max;
                DumpSoundsProgressBar.Value = Progress;
                if (Progress == Max)
                    DumpSoundsProgressBar.Visibility = Visibility.Hidden;
            }));
        }

        public MainWindow()
        {
            InitializeComponent();
            Core.Init();
            ImageBank.OnImageLoaded += (current, all) =>
            {
                loadMax[0] = all;
                UpdateProgress(1);
            };
            GameData.OnChunkLoaded += (current, all) =>
            {
                loadMax[1] = all;
                UpdateProgress(1);
            };
            GameData.OnFrameLoaded += (current, all) =>
            {
                loadMax[2] = all;
                UpdateProgress(1);
            };
            SoundBank.OnSoundLoaded += (current, all) =>
            {
                loadMax[3] = all;
                UpdateProgress(1);
            };/*
            Logger.onLog += (log) =>
            {
                ConsoleTextBox.AppendText(log);
            };*/

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
            MFADumpTabButton.Visibility = Visibility.Visible;
            PackDataTabButton.Visibility = Visibility.Visible;
            ObjectsTabButton.Visibility = Visibility.Visible;
            SoundsTabButton.Visibility = Visibility.Visible;
            PluginsTabButton.Visibility = Visibility.Visible;

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
                $"Number of Images: {game?.Images?.Items.Count ?? 0}\n" +
                $"Number of Sounds: {game?.Sounds?.Items.Count ?? 0}\n" +
                $"Number of Music: {game?.Music?.Items.Count ?? 0}\n" +
                $"Unique FrameItems: {game?.frameitems?.Count ?? 0}\n" +
                $"Frame Count: {game.frames.Count}\n";

            MFATreeView.Items.Clear();
            foreach (var frame in game.frames)
            {
                TreeViewItem frameItem = new TreeViewItem();
                frameItem.Header = frame.name;
                frameItem.Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFDF7226"));
                frameItem.FontFamily = new System.Windows.Media.FontFamily("Courier New");
                frameItem.FontSize = 14;
                frameItem.Padding = new Thickness(1, 1, 0, 0);
                MFATreeView.Items.Add(frameItem);
                foreach (var item in frame.objects)
                {
                    TreeViewItem objectItem = new TreeViewItem();
                    objectItem.Header = game.frameitems[item.objectInfo].name;
                    objectItem.Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFDF7226"));
                    objectItem.FontFamily = new System.Windows.Media.FontFamily("Courier New");
                    objectItem.FontSize = 14;
                    objectItem.Padding = new Thickness(1, 1, 0, 0);
                    frameItem.Items.Add(objectItem);
                }

            }

            SoundsTreeView.Items.Clear();
            int soundCount = 0;
            try
            {
                foreach (var sound in game.Sounds.Items)
                {
                    TreeViewItem soundItem = new TreeViewItem();
                    soundItem.Header = sound.Name;
                    soundItem.Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFDF7226"));
                    soundItem.FontFamily = new System.Windows.Media.FontFamily("Courier New");
                    soundItem.FontSize = 14;
                    soundItem.Padding = new Thickness(1, 1, 0, 0);
                    soundItem.Tag = soundCount;
                    SoundsTreeView.Items.Add(soundItem);
                    soundCount++;
                }
            }
            catch { }

            loadingWindow.Close();
            Directory.CreateDirectory("Dumps");
        }

        public static IFileReader currentReader;
        private void SelectFileButton_OnClick(object sender, RoutedEventArgs e)
        {
            var fileSelector = new OpenFileDialog();
            fileSelector.Title = "Select game";
            fileSelector.CheckFileExists = true;
            fileSelector.CheckPathExists = true;
            fileSelector.Filter = @"Fusion executable|*.exe|Fusion CCN|*.ccn|Fusion Android app|*.apk|Fusion Switch app|*.dat";
            if (fileSelector.ShowDialog().Value)
            {
                if (fileSelector.FileName.EndsWith(".exe"))
                    currentReader = new ExeFileReader();
                else
                    currentReader = new CCNFileReader();

                Core.parameters = "";

                if (fileSelector.FileName.EndsWith(".apk"))
                    Core.path = ApkFileReader.ExtractCCN(fileSelector.FileName);
                else
                    Core.path = fileSelector.FileName;

                loadingWindow = new LoadingWindow();
                loadingWindow.Owner = this;
                loadingWindow.Show();
                var backgroundWorker = new BackgroundWorker();
                backgroundWorker.DoWork += (o,e) =>
                {
                    Console.WriteLine($"Reading game with \"{currentReader.Name}\"");
                    currentReader.PatchMethods();
                    currentReader.LoadGame(Core.path);
                };
                backgroundWorker.RunWorkerCompleted += (o, e) =>
                {
                    AfterGUILoaded();
                };
                
                backgroundWorker.RunWorkerAsync();
            }
        }

        private void DumpImages(object sender, RoutedEventArgs e)
        {
            List<IFusionTool> availableTools = new List<IFusionTool>();
            var newAsm = Assembly.LoadFrom("Plugins\\Dumper.dll");
            foreach (var pluginType in newAsm.GetTypes())
            {
                if (pluginType.GetInterface(typeof(IFusionTool).FullName) != null)
                    availableTools.Add((IFusionTool)Activator.CreateInstance(pluginType));
            }
            imageDumper = availableTools[1];
            DumpImagesProgressBar.Visibility = Visibility.Visible;
            Thread dumpImagesThread = new Thread(DumpImagesThread);
            dumpImagesThread.Name = "Dump Images";
            dumpImagesThread.Start();
            Thread dumpImagesProgThread = new Thread(DumpImagesProgThread);
            dumpImagesProgThread.Name = "Dump Images Progress";
            dumpImagesProgThread.Start();
        }
        
        void DumpImagesThread()
        {
            imageDumper.Execute(currentReader);
        }
        
        void DumpImagesProgThread()
        {
            while (imageDumper != null)
            {
                if (imageDumper.Progress.Length == 2)
                {
                    UpdateImageProgress(imageDumper.Progress[0], imageDumper.Progress[1]);
                    if (imageDumper.Progress[0] == imageDumper.Progress[1])
                        break;
                }
            }
            UpdateImageProgress(currentReader.getGameData().Images.Items.Count, currentReader.getGameData().Images.Items.Count);
            imageDumper = null;
        }

        private void DumpSounds(object sender, RoutedEventArgs e)
        {
            List<IFusionTool> availableTools = new List<IFusionTool>();
            var newAsm = Assembly.LoadFrom("Plugins\\Dumper.dll");
            foreach (var pluginType in newAsm.GetTypes())
            {
                if (pluginType.GetInterface(typeof(IFusionTool).FullName) != null)
                    availableTools.Add((IFusionTool)Activator.CreateInstance(pluginType));
            }
            soundDumper = availableTools[2];
            DumpSoundsProgressBar.Visibility = Visibility.Visible;
            Thread dumpSoundsThread = new Thread(DumpSoundsThread);
            dumpSoundsThread.Name = "Dump Sounds";
            dumpSoundsThread.Start();
            Thread dumpSoundsProgThread = new Thread(DumpSoundsProgThread);
            dumpSoundsProgThread.Name = "Dump Sounds Progress";
            dumpSoundsProgThread.Start();
        }

        void DumpSoundsThread()
        {
            soundDumper.Execute(currentReader);
        }

        void DumpSoundsProgThread()
        {
            while (soundDumper != null)
            {
                if (soundDumper.Progress.Length == 2)
                {
                    UpdateSoundProgress(soundDumper.Progress[0], soundDumper.Progress[1]);
                    if (soundDumper.Progress[0] == soundDumper.Progress[1])
                        break;
                }
            }
            UpdateSoundProgress(currentReader.getGameData().Sounds.Items.Count, currentReader.getGameData().Sounds.Items.Count);
            soundDumper = null;
        }

        private void SoundTreeViewChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            TreeViewItem SelectedItem = (TreeViewItem)SoundsTreeView.SelectedItem;
            PlaySoundButton.Visibility = Visibility.Visible;
            PlaySoundButton.Content = "Play Sound";
            if (CurrentPlayingSound != null)
                PlaySound(sender, e);
            SoundInfoText.Content = $"Name: {currentReader.getGameData().Sounds.Items[int.Parse(SelectedItem.Tag.ToString())].Name}\nSize: {currentReader.getGameData().Sounds.Items[int.Parse(SelectedItem.Tag.ToString())].Size}kb";
        }

        private void PlaySound(object sender, RoutedEventArgs e)
        {
            if (CurrentPlayingSound == null)
            {
                PlaySoundButton.Content = "Stop Sound";
                MemoryStream bytes = new MemoryStream(currentReader.getGameData().Sounds.Items[int.Parse(((TreeViewItem)SoundsTreeView.SelectedItem).Tag.ToString())].Data);
                CurrentPlayingSound = new SoundPlayer(bytes);
                CurrentPlayingSound.Play();
            }
            else
            {
                PlaySoundButton.Content = "Play Sound";
                CurrentPlayingSound.Stop();
                CurrentPlayingSound = null;
            }
        }
    }
}
