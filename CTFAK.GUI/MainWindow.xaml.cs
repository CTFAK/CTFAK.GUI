using CTFAK;
using CTFAK.CCN;
using CTFAK.CCN.Chunks.Banks;
using CTFAK.CCN.Chunks.Objects;
using CTFAK.EXE;
using CTFAK.FileReaders;
using CTFAK.Tools;
using CTFAK.Utils;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Action = System.Action;

namespace Legacy_CTFAK_UI
{
    public partial class MainWindow : Window
    {
        public static IFileReader currentReader;
        public IFusionTool sortedImageDumper;
        public IFusionTool imageDumper;
        public IFusionTool soundDumper;
        public IFusionTool Decompiler;
        public int curAnimFrame;
        public SoundPlayer CurrentPlayingSound;
        public static string Color = "#FFDF7226";

        public void UpdateProgress(double incrementBy, double maximum, string loadType)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate ()
            {
                if (LoadingProgressBarOne.Maximum != maximum)
                    LoadingProgressBarOne.Value = 0;
                LoadingProgressBarOne.Maximum = maximum;
                LoadingProgressBarOne.Value += incrementBy;
                int percentage = (int)(LoadingProgressBarOne.Value / LoadingProgressBarOne.Maximum * 100);
                if (percentage < 0 || percentage > 100)
                    percentage = 100;
                LoadingDesc.Content = $"Loading {loadType}. {percentage}%";
            }));
        }

        public void UpdateSortedImageProgress(float Progress, float Max)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate ()
            {
                DumpSortedImagesProgressBar.Maximum = Max;
                DumpSortedImagesProgressBar.Value = Progress;
                if (Progress == Max)
                {
                    DumpSortedImagesProgressBar.Visibility = Visibility.Hidden;
                    DumpImagesButton.IsEnabled = true;
                    DumpSortedImagesButton.IsEnabled = true;
                }
            }));
        }

        public void UpdateImageProgress(float Progress, float Max)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate()
            {
                DumpImagesProgressBar.Maximum = Max;
                DumpImagesProgressBar.Value = Progress;
                if (Progress == Max)
                {
                    DumpImagesProgressBar.Visibility = Visibility.Hidden;
                    DumpImagesButton.IsEnabled = true;
                    DumpSortedImagesButton.IsEnabled = true;
                }
            }));
        }

        public void UpdateSoundProgress(float Progress, float Max)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate ()
            {
                DumpSoundsProgressBar.Maximum = Max;
                DumpSoundsProgressBar.Value = Progress;
                if (Progress == Max)
                {
                    DumpSoundsProgressBar.Visibility = Visibility.Hidden;
                    DumpSoundsButton.IsEnabled = true;
                }
            }));
        }

        public MainWindow()
        {
            InitializeComponent();
            Core.Init();
            ImageBank.OnImageLoaded += (current, all) =>
            {
                UpdateProgress(1, all, "images");
            };
            GameData.OnChunkLoaded += (current, all) =>
            {
                UpdateProgress(1, all, "chunks");
            };
            GameData.OnFrameLoaded += (current, all) =>
            {
                UpdateProgress(1, all, "frames");
            };
            SoundBank.OnSoundLoaded += (current, all) =>
            {
                UpdateProgress(1, all, "sounds");
            };
            /*Logger.onLog += (log) =>
            {
                ConsoleTextBox.AppendText(log);
            };*/

            /*List<LoadPlugin> availableTools = new List<LoadPlugin>();
            int toolID = 0;
            foreach (var item in Directory.GetFiles("Plugins", "*.dll"))
            {
                var newAsm = Assembly.LoadFrom(Path.GetFullPath(item));
                foreach (var pluginType in newAsm.GetTypes())
                {
                    if (pluginType.GetInterface(typeof(LoadPlugin).FullName) != null)
                    {
                        availableTools.Add((LoadPlugin)Activator.CreateInstance(pluginType));
                        TreeViewItem TreeItem = new TreeViewItem();
                        TreeItem.Header = pluginType.GetInterface(typeof(LoadPlugin).FullName);
                        TreeItem.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                        TreeItem.FontFamily = new FontFamily("Courier New");
                        TreeItem.FontSize = 14;
                        TreeItem.Padding = new Thickness(1, 1, 0, 0);
                        TreeItem.Tag = toolID;
                        PluginsTreeView.Items.Add(TreeItem);
                        toolID++;
                    }
                }
            }*/

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
            DumpSortedImagesButton.Visibility = Visibility.Visible;
            DumpImagesButton.Visibility = Visibility.Visible;
            DumpSoundsButton.Visibility = Visibility.Visible;
            DumpMusicButton.Visibility = Visibility.Visible;
            MFATreeView.Visibility = Visibility.Visible;
            var game = currentReader.getGameData();
            MFAInfoTextBlock.Content =
                $"Title: {game.name ?? ""}\n" +
                $"Copyright: {game.copyright ?? ""}\n" +
                //$"Product Version: to be filled\n" +
                $"Build: {Settings.Build}\n" +
                //$"Runtime Version: to be filled\n
                "\n" +
                $"Number of Frames: {game.frames.Count}\n" +
                $"Number of Objects: {game?.frameitems?.Count ?? 0}\n" +
                $"Number of Images: {game?.Images?.Items.Count ?? 0}\n" +
                $"Number of Sounds: {game?.Sounds?.Items.Count ?? 0}\n" +
                $"Number of Music: {game?.Music?.Items.Count ?? 0}\n";

            int frameCount = 0;
            ObjectsTreeView.Items.Clear();
            MFATreeView.Items.Clear();
            TreeViewItem FrameParent = new TreeViewItem();
            FrameParent.Header = $"Frames";
            FrameParent.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
            FrameParent.FontFamily = new FontFamily("Courier New");
            FrameParent.FontSize = 14;
            FrameParent.Padding = new Thickness(1, 1, 0, 0);
            MFATreeView.Items.Add(FrameParent);
            foreach (var frame in game.frames)
            {
                if (frame.name == null || frame.name.Length == 0) continue;
                TreeViewItem frameItem = new TreeViewItem();
                TreeViewItem frameItem2 = new TreeViewItem();
                frameItem.Header = frame.name;
                frameItem2.Header = frame.name;
                frameItem.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                frameItem2.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                frameItem.FontFamily = new FontFamily("Courier New");
                frameItem2.FontFamily = new FontFamily("Courier New");
                frameItem.FontSize = 14;
                frameItem2.FontSize = 14;
                frameItem.Padding = new Thickness(1, 1, 0, 0);
                frameItem2.Padding = new Thickness(1, 1, 0, 0);
                frameItem.Tag = $"{frameCount}Frame";
                frameItem2.Tag = $"{frameCount}Frame";
                FrameParent.Items.Add(frameItem);
                ObjectsTreeView.Items.Add(frameItem2);
                frameCount++;
                foreach (var item in frame.objects)
                {
                    TreeViewItem objectItem = new TreeViewItem();
                    TreeViewItem objectItem2 = new TreeViewItem();
                    objectItem.Header = game.frameitems[item.objectInfo].name;
                    objectItem2.Header = game.frameitems[item.objectInfo].name;
                    objectItem.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                    objectItem2.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                    objectItem.FontFamily = new FontFamily("Courier New");
                    objectItem2.FontFamily = new FontFamily("Courier New");
                    objectItem.FontSize = 14;
                    objectItem2.FontSize = 14;
                    objectItem.Padding = new Thickness(1, 1, 0, 0);
                    objectItem2.Padding = new Thickness(1, 1, 0, 0);
                    objectItem.Tag = $"{item.objectInfo}Object";
                    objectItem2.Tag = $"{item.objectInfo}Object";
                    frameItem.Items.Add(objectItem);
                    frameItem2.Items.Add(objectItem2);
                    if (game.frameitems[item.objectInfo].properties is ObjectCommon common)
                    {
                        if (common.Identifier != "SPRI" && common.Identifier != "SP" && common.Parent.ObjectType != 2) continue;
                        int i = 0;
                        foreach (var anim in common.Animations.AnimationDict)
                        {
                            if (common.Animations.AnimationDict[i].DirectionDict == null)
                            {
                                i++;
                                continue;
                            }
                            TreeViewItem animItem = new TreeViewItem();
                            animItem.Header = $"Animation {i}";
                            animItem.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                            animItem.FontFamily = new FontFamily("Courier New");
                            animItem.FontSize = 14;
                            animItem.Padding = new Thickness(1, 1, 0, 0);
                            animItem.Tag = $"{anim.Key}Animation";
                            objectItem2.Items.Add(animItem);
                            i++;
                        }
                        if (objectItem2.Items.Count <= 1) objectItem2.Items.Clear();
                    }
                    
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
                    soundItem.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                    soundItem.FontFamily = new FontFamily("Courier New");
                    soundItem.FontSize = 14;
                    soundItem.Padding = new Thickness(1, 1, 0, 0);
                    soundItem.Tag = soundCount;
                    SoundsTreeView.Items.Add(soundItem);
                    soundCount++;
                }
            }
            catch { }

            LoadingGrid.Visibility = Visibility.Hidden;
            LoadingProgressBarOne.Maximum = 0.1;
            LoadingProgressBarOne.Value = 0;
            Directory.CreateDirectory("Dumps");
        }

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

                LoadingGrid.Visibility = Visibility.Visible;
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
            DumpImagesButton.IsEnabled = false;
            DumpSortedImagesButton.IsEnabled = false;
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
            DumpSoundsButton.IsEnabled = false;
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
            if (SoundsTreeView.Items.Count == 0) return;
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

        private void UpdateSettings(object sender, RoutedEventArgs e)
        {
            if (Color != "#" + ColorTextBox.Text && ColorTextBox.Text.Length == 6)
            {
                Color = "#" + ColorTextBox.Text;
                //Console
                ConsoleTextBox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                ConsoleLabel.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                //Main
                VersionLabel.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                SelectFileButton.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                SelectFileButton.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                MFAInfoTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                OpenDumpFolderButton.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                OpenDumpFolderButton.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                DumpSortedImagesButton.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                DumpSortedImagesButton.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                DumpImagesButton.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                DumpImagesButton.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                DumpSoundsButton.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                DumpSoundsButton.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                DumpMusicButton.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                DumpMusicButton.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                ItemInfoTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                DumpSortedImagesProgressBar.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                DumpImagesProgressBar.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                DumpSoundsProgressBar.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                DumpMusicProgressBar.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                foreach (TreeViewItem item in MFATreeView.Items)
                {
                    item.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                    foreach (TreeViewItem subitem in item.Items)
                    {
                        subitem.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                        foreach (TreeViewItem obj in subitem.Items)
                            obj.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                    }
                }
                //MFA Dump
                DumpWarningLabel.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                DumpMFAButton.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                DumpMFAButton.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                ExtensionsCheckbox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                IconsCheckbox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                ImagesCheckbox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                EventsCheckbox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                TraceCheckbox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                //Pack Dump
                DumpPackedButton.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                DumpPackedButton.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                DumpAllPackedButton.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                DumpAllPackedButton.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                PackedInfoText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                PackDataInfoText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                foreach (TreeViewItem item in PackedTreeView.Items)
                    item.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                //Objects
                DumpSelectedButton.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                DumpSelectedButton.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                PlayAnimationButton.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                PlayAnimationButton.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                AnimationLeft.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                AnimationLeft.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                AnimationRight.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                AnimationRight.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                AnimationCurrentFrame.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                ObjectInfoText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                foreach (TreeViewItem item in ObjectsTreeView.Items)
                {
                    item.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                    foreach (TreeViewItem obj in item.Items)
                    {
                        obj.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                        foreach (TreeViewItem obj2 in obj.Items)
                            obj2.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                    }
                }
                //Sounds
                PlaySoundButton.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                PlaySoundButton.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                SoundInfoText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                SoundsInfoText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                foreach (TreeViewItem item in SoundsTreeView.Items)
                    item.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                //Plugins
                ActivateButton.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                ActivateButton.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                PluginInfoText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                PluginsInfoText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                foreach (TreeViewItem item in PluginsTreeView.Items)
                    item.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                //Settings
                UpdateButton.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                UpdateButton.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                SettingsInfoText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                ColorTextBox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                //Loading Window
                LoadingGrid.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                LoadingLabel.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                LoadingProgressBarOne.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                LoadingDesc.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
            }
        }

        private void DumpSortedImages(object sender, RoutedEventArgs e)
        {
            List<IFusionTool> availableTools = new List<IFusionTool>();
            var newAsm = Assembly.LoadFrom("Plugins\\Dumper.dll");
            foreach (var pluginType in newAsm.GetTypes())
            {
                if (pluginType.GetInterface(typeof(IFusionTool).FullName) != null)
                    availableTools.Add((IFusionTool)Activator.CreateInstance(pluginType));
            }
            sortedImageDumper = availableTools[3];
            DumpImagesButton.IsEnabled = false;
            DumpSortedImagesButton.IsEnabled = false;
            DumpSortedImagesProgressBar.Visibility = Visibility.Visible;
            Thread dumpSortedImagesThread = new Thread(DumpSortedImagesThread);
            dumpSortedImagesThread.Name = "Dump Sorted Images";
            dumpSortedImagesThread.Start();
            Thread dumpSortedImagesProgThread = new Thread(DumpSortedImagesProgThread);
            dumpSortedImagesProgThread.Name = "Dump Sorted Images Progress";
            dumpSortedImagesProgThread.Start();
        }

        void DumpSortedImagesThread()
        {
            sortedImageDumper.Execute(currentReader);
        }

        void DumpSortedImagesProgThread()
        {
            while (sortedImageDumper != null)
            {
                if (sortedImageDumper.Progress.Length == 2)
                {
                    UpdateSortedImageProgress(sortedImageDumper.Progress[0], sortedImageDumper.Progress[1]);
                    if (sortedImageDumper.Progress[0] == sortedImageDumper.Progress[1])
                        break;
                }
            }
            UpdateSortedImageProgress(currentReader.getGameData().Images.Items.Count, currentReader.getGameData().Images.Items.Count);
            imageDumper = null;
        }

        private void ParameterToggle(object sender, RoutedEventArgs e)
        {
            CheckBox cb = sender as CheckBox;
            if ((bool)cb.IsChecked)
            {
                Core.parameters = Core.parameters + cb.Tag.ToString();
            }
            else
            {
                Core.parameters.Replace(cb.Tag.ToString(), "");
            }
            ConsoleTextBox.Text = Core.parameters;
        }

        private void DumpMFAButton_Click(object sender, RoutedEventArgs e)
        {
            List<IFusionTool> availableTools = new List<IFusionTool>();
            var newAsm = Assembly.LoadFrom("Plugins\\CTFAK.Decompiler.dll");
            foreach (var pluginType in newAsm.GetTypes())
            {
                if (pluginType.GetInterface(typeof(IFusionTool).FullName) != null)
                    availableTools.Add((IFusionTool)Activator.CreateInstance(pluginType));
            }
            Decompiler = availableTools[0];
            DumpMFAButton.IsEnabled = false;
            Thread decompilerThread = new Thread(DecompilerThread);
            decompilerThread.Name = "Decompiler";
            decompilerThread.Start();
        }

        void DecompilerThread()
        {
            Decompiler.Execute(currentReader);
            DecompilerFinished();
        }

        public void DecompilerFinished()
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate ()
            {
                DumpMFAButton.IsEnabled = true;
            }));
        }

        private void SelectObject(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (ObjectsTreeView.Items.Count == 0) return;
            TreeViewItem SelectedItem = (TreeViewItem)ObjectsTreeView.SelectedItem;
            if (SelectedItem.Tag.ToString().Contains("Object"))
            {
                AnimationLeft.Visibility = Visibility.Visible;
                AnimationRight.Visibility = Visibility.Visible;
            }
            else if (SelectedItem.Tag.ToString().Contains("Animation"))
            {
                AnimationLeft.Visibility = Visibility.Visible;
                AnimationRight.Visibility = Visibility.Visible;
                TreeViewItem ItemParent = (TreeViewItem)SelectedItem.Parent;
                var animInfo = currentReader.getGameData().frameitems[int.Parse(ItemParent.Tag.ToString().Replace("Object", ""))];
                if (animInfo.properties is ObjectCommon anim)
                {
                    var frm = anim.Animations.AnimationDict[int.Parse(SelectedItem.Tag.ToString().Replace("Animation", ""))].DirectionDict[0].Frames;
                    System.Drawing.Bitmap bmp = null;
                    try
                    {
                        curAnimFrame = 0;
                        bmp = currentReader.getGameData().Images.Items[frm[curAnimFrame]].bitmap;
                        var handle = bmp.GetHbitmap();
                        ObjectPicture.Source = Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                        UpdateImagePreview();
                        AnimationCurrentFrame.Content = $"{curAnimFrame + 1}/{anim.Animations.AnimationDict[int.Parse(SelectedItem.Tag.ToString().Replace("Animation", ""))].DirectionDict[0].Frames.Count}";
                    }
                    catch (Exception exc) { Logger.Log(exc); }
                    try
                    {
                        ObjectInfoText.Content =
                            $"Name: {ItemParent.Header}\n" +
                            $"Type: Active\n" +
                            $"Size: {bmp.Width}x{bmp.Height}\n" +
                            $"Animations: {anim.Animations.AnimationDict.Count}";
                    }
                    catch (Exception exc) { Logger.Log(exc); }
                }
                return;
            }
            else
            {
                AnimationLeft.Visibility = Visibility.Hidden;
                AnimationRight.Visibility = Visibility.Hidden;
                AnimationCurrentFrame.Content = "";
                ObjectPicture.Source = null;
                try
                {
                    ObjectInfoText.Content =
                        $"Name: {SelectedItem.Header}\n" +
                        $"Type: Frame\n" +
                        $"Objects: {SelectedItem.Items.Count}";
                }
                catch (Exception exc) { Logger.Log(exc); }
                return;
            }
            var objectInfo = currentReader.getGameData().frameitems[int.Parse(SelectedItem.Tag.ToString().Replace("Object", ""))];

            if (objectInfo.properties is Backdrop bd)
            {
                AnimationLeft.Visibility = Visibility.Hidden;
                AnimationRight.Visibility = Visibility.Hidden;
                if (bd.Image == null) return;
                System.Drawing.Bitmap bmp = null;
                try
                {
                    bmp = currentReader.getGameData().Images.Items[bd.Image].bitmap;
                    var handle = bmp.GetHbitmap();
                    ObjectPicture.Source = Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                    UpdateImagePreview();
                    AnimationCurrentFrame.Content = "";
                }
                catch (Exception exc) { Logger.Log(exc); }
                try
                {
                    ObjectInfoText.Content =
                        $"Name: {objectInfo.name}\n" +
                        $"Type: Backdrop\n" +
                        $"Size: {bmp.Width}x{bmp.Height}";
                }
                catch (Exception exc) { Logger.Log(exc); }
            }

            if (objectInfo.properties is Quickbackdrop qbd)
            {
                AnimationLeft.Visibility = Visibility.Hidden;
                AnimationRight.Visibility = Visibility.Hidden;
                if (qbd.Image == null) return;
                System.Drawing.Bitmap bmp = null;
                try
                {
                    bmp = currentReader.getGameData().Images.Items[qbd.Image].bitmap;
                    var handle = bmp.GetHbitmap();
                    ObjectPicture.Source = Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                    UpdateImagePreview();
                    AnimationCurrentFrame.Content = "";
                }
                catch (Exception exc) { Logger.Log(exc); }
                try
                {
                    ObjectInfoText.Content =
                        $"Name: {objectInfo.name}\n" +
                        $"Type: Quick Backdrop\n" +
                        $"Size: {bmp.Width}x{bmp.Height}";
                }
                catch (Exception exc) { Logger.Log(exc); }
            }

            if (objectInfo.properties is ObjectCommon common)
            {
                if (common.Identifier == "SPRI" || common.Identifier == "SP" || !Settings.twofiveplus && common.Parent.ObjectType == 2)
                {
                    AnimationLeft.Visibility = Visibility.Visible;
                    AnimationRight.Visibility = Visibility.Visible;
                    if (common.Animations.AnimationDict == null) return;
                    if (common.Animations.AnimationDict[0].DirectionDict == null) return;
                    if (common.Animations.AnimationDict[0].DirectionDict[0].Frames == null) return;
                    var frm = common.Animations.AnimationDict[0].DirectionDict[0].Frames;
                    System.Drawing.Bitmap bmp = null;
                    try
                    {
                        curAnimFrame = 0;
                        bmp = currentReader.getGameData().Images.Items[frm[curAnimFrame]].bitmap;
                        var handle = bmp.GetHbitmap();
                        ObjectPicture.Source = Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                        UpdateImagePreview();
                        AnimationCurrentFrame.Content = $"{curAnimFrame + 1}/{common.Animations.AnimationDict[0].DirectionDict[0].Frames.Count}";
                    }
                    catch (Exception exc) { Logger.Log(exc); }
                    try
                    {
                        ObjectInfoText.Content =
                            $"Name: {objectInfo.name}\n" +
                            $"Type: Active\n" +
                            $"Size: {bmp.Width}x{bmp.Height}\n" +
                            $"Animations: {common.Animations.AnimationDict.Count}";
                    }
                    catch (Exception exc) { Logger.Log(exc); }
                }
                else if (common.Identifier == "CNTR" || common.Identifier == "CN" || !Settings.twofiveplus && common.Parent.ObjectType == 7)
                {
                    AnimationLeft.Visibility = Visibility.Visible;
                    AnimationRight.Visibility = Visibility.Visible;
                    var counter = common.Counters;
                    if (counter == null)
                    {
                        AnimationLeft.Visibility = Visibility.Hidden;
                        AnimationRight.Visibility = Visibility.Hidden;
                        ObjectPicture.Source = null;
                        AnimationCurrentFrame.Content = "";
                        ObjectInfoText.Content =
                            $"Name: {objectInfo.name}\n" +
                            $"Type: Counter\n";
                        return;
                    }
                    if (!(counter.DisplayType == 1 || counter.DisplayType == 4 || counter.DisplayType == 50)) return;
                    if (counter.Frames == null) return;
                    var frm = counter.Frames;
                    System.Drawing.Bitmap bmp = null;
                    try
                    {
                        curAnimFrame = 0;
                        bmp = currentReader.getGameData().Images.Items[frm[curAnimFrame]].bitmap;
                        var handle = bmp.GetHbitmap();
                        ObjectPicture.Source = Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                        UpdateImagePreview();
                        AnimationCurrentFrame.Content = $"{curAnimFrame + 1}/{counter.Frames.Count}";
                    }
                    catch (Exception exc) { Logger.Log(exc); }
                    try
                    {
                        ObjectInfoText.Content =
                            $"Name: {objectInfo.name}\n" +
                            $"Type: Counter\n" +
                            $"Size: {bmp.Width}x{bmp.Height}";
                    }
                    catch (Exception exc) { Logger.Log(exc); }
                }
                else if (common.Identifier == "TEXT" || common.Identifier == "TE" || !Settings.twofiveplus && common.Parent.ObjectType == 3)
                {
                    AnimationLeft.Visibility = Visibility.Hidden;
                    AnimationRight.Visibility = Visibility.Hidden;
                    ObjectPicture.Source = null;
                    AnimationCurrentFrame.Content = "";
                    System.Drawing.Bitmap bmp = new System.Drawing.Bitmap((int)ObjectPicture.Width, (int)ObjectPicture.Height);
                    try
                    {
                        System.Drawing.RectangleF rectf = new System.Drawing.RectangleF(0, 0, bmp.Width, bmp.Height);
                        System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bmp);
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                        System.Drawing.StringFormat format = new System.Drawing.StringFormat()
                        {
                            Alignment = System.Drawing.StringAlignment.Center,
                            LineAlignment = System.Drawing.StringAlignment.Center
                        };

                        System.Drawing.SolidBrush brush = new System.Drawing.SolidBrush(System.Drawing.ColorTranslator.FromHtml(Color));
                        g.DrawString(common.Text.Items[curAnimFrame].Value, new System.Drawing.Font("Courier New", (int)ObjectPicture.Width / 25, System.Drawing.FontStyle.Bold), brush, rectf, format);
                        g.Flush();

                        var handle = bmp.GetHbitmap();
                        ObjectPicture.Source = Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                        UpdateImagePreview();
                        AnimationCurrentFrame.Content = $"{curAnimFrame + 1}/{common.Text.Items.Count}";
                    }
                    catch (Exception exc) { Logger.Log(exc); }
                    try
                    {
                        ObjectInfoText.Content =
                            $"Name: {objectInfo.name}\n" +
                            $"Type: String\n"/* +
                            $"Paragraphs: {common.Text.Items.Count}"*/;
                    }
                    catch (Exception exc) { Logger.Log(exc); }
                }
                else
                {
                    AnimationLeft.Visibility = Visibility.Visible;
                    AnimationRight.Visibility = Visibility.Visible;
                    ObjectPicture.Source = null;
                    AnimationCurrentFrame.Content = "";
                    try
                    {
                        ObjectInfoText.Content =
                            $"Name: {objectInfo.name}\n" +
                            $"Identifier: {common.Identifier}\n";
                    }
                    catch (Exception exc) { Logger.Log(exc); }
                }
            }
            //MemoryStream bytes = new MemoryStream(currentReader.getGameData().frameitems[int.Parse(SelectedItem.Tag.ToString())]));
            
        }

        private void AnimationLeft_Click(object sender, RoutedEventArgs e)
        {
            if (ObjectsTreeView.Items.Count == 0) return;
            TreeViewItem SelectedItem = (TreeViewItem)ObjectsTreeView.SelectedItem;
            if (SelectedItem != null)
            {
                if (SelectedItem.Tag.ToString().Contains("Animation"))
                {
                    AnimationLeft.Visibility = Visibility.Visible;
                    AnimationRight.Visibility = Visibility.Visible;
                    TreeViewItem ItemParent = (TreeViewItem)SelectedItem.Parent;
                    var animInfo = currentReader.getGameData().frameitems[int.Parse(ItemParent.Tag.ToString().Replace("Object", ""))];
                    if (animInfo.properties is ObjectCommon anim)
                    {
                        curAnimFrame--;
                        if (curAnimFrame < 0) curAnimFrame = anim.Animations.AnimationDict[int.Parse(SelectedItem.Tag.ToString().Replace("Animation", ""))].DirectionDict[0].Frames.Count - 1;
                        var frm = anim.Animations.AnimationDict[int.Parse(SelectedItem.Tag.ToString().Replace("Animation", ""))].DirectionDict[0].Frames;
                        System.Drawing.Bitmap bmp = null;
                        try
                        {
                            bmp = currentReader.getGameData().Images.Items[frm[curAnimFrame]].bitmap;
                            var handle = bmp.GetHbitmap();
                            ObjectPicture.Source = Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                            UpdateImagePreview();
                            AnimationCurrentFrame.Content = $"{curAnimFrame + 1}/{anim.Animations.AnimationDict[int.Parse(SelectedItem.Tag.ToString().Replace("Animation", ""))].DirectionDict[0].Frames.Count}";
                        }
                        catch (Exception exc) { Logger.Log(exc); }
                        try
                        {
                            ObjectInfoText.Content =
                                $"Name: {ItemParent.Header}\n" +
                                $"Type: Active\n" +
                                $"Size: {bmp.Width}x{bmp.Height}\n" +
                                $"Animations: {anim.Animations.AnimationDict.Count}";
                        }
                        catch (Exception exc) { Logger.Log(exc); }
                    }
                    return;
                }
                if (currentReader.getGameData().frameitems[int.Parse(SelectedItem.Tag.ToString().Replace("Object", ""))].properties is ObjectCommon common)
                {
                    if (common.Identifier == "SPRI" || common.Identifier == "SP" || !Settings.twofiveplus && common.Parent.ObjectType == 2)
                    {
                        if (common.Animations?.AnimationDict[0] == null) return;
                        if (common.Animations?.AnimationDict[0].DirectionDict[0] == null) return;
                        if (common.Animations?.AnimationDict[0].DirectionDict[0].Frames[0] == null) return;
                        curAnimFrame--;
                        if (curAnimFrame < 0) curAnimFrame = common.Animations.AnimationDict[0].DirectionDict[0].Frames.Count - 1;
                        var frm = common.Animations.AnimationDict[0].DirectionDict[0].Frames[curAnimFrame];
                        try
                        {
                            var handle = currentReader.getGameData().Images.Items[frm].bitmap.GetHbitmap();
                            ObjectPicture.Source = Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                            UpdateImagePreview();
                            AnimationCurrentFrame.Content = $"{curAnimFrame + 1}/{common.Animations.AnimationDict[0].DirectionDict[0].Frames.Count}";
                        }
                        catch (Exception exc) { Logger.Log(exc); }
                    }
                    else if (common.Identifier == "CNTR" || common.Identifier == "CN" || !Settings.twofiveplus && common.Parent.ObjectType == 7)
                    {
                        var counter = common.Counters;
                        if (counter == null) return;
                        if (!(counter.DisplayType == 1 || counter.DisplayType == 4 || counter.DisplayType == 50)) return;
                        if (counter.Frames == null) return;
                        curAnimFrame--;
                        if (curAnimFrame < 0) curAnimFrame = counter.Frames.Count - 1;
                        var frm = counter.Frames;
                        try
                        {
                            var handle = currentReader.getGameData().Images.Items[frm[curAnimFrame]].bitmap.GetHbitmap();
                            ObjectPicture.Source = Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                            UpdateImagePreview();
                            AnimationCurrentFrame.Content = $"{curAnimFrame + 1}/{counter.Frames.Count}";
                        }
                        catch (Exception exc) { Logger.Log(exc); }
                    }
                }
            }
        }

        private void AnimationRight_Click(object sender, RoutedEventArgs e)
        {
            if (ObjectsTreeView.Items.Count == 0) return;
            TreeViewItem SelectedItem = (TreeViewItem)ObjectsTreeView.SelectedItem;
            if (SelectedItem != null)
            {
                if (SelectedItem.Tag.ToString().Contains("Animation"))
                {
                    AnimationLeft.Visibility = Visibility.Visible;
                    AnimationRight.Visibility = Visibility.Visible;
                    TreeViewItem ItemParent = (TreeViewItem)SelectedItem.Parent;
                    var animInfo = currentReader.getGameData().frameitems[int.Parse(ItemParent.Tag.ToString().Replace("Object", ""))];
                    if (animInfo.properties is ObjectCommon anim)
                    {
                        curAnimFrame++;
                        if (curAnimFrame > anim.Animations.AnimationDict[int.Parse(SelectedItem.Tag.ToString().Replace("Animation", ""))].DirectionDict[0].Frames.Count - 1) curAnimFrame = 0;
                        var frm = anim.Animations.AnimationDict[int.Parse(SelectedItem.Tag.ToString().Replace("Animation", ""))].DirectionDict[0].Frames;
                        System.Drawing.Bitmap bmp = null;
                        try
                        {
                            bmp = currentReader.getGameData().Images.Items[frm[curAnimFrame]].bitmap;
                            var handle = bmp.GetHbitmap();
                            ObjectPicture.Source = Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                            UpdateImagePreview();
                            AnimationCurrentFrame.Content = $"{curAnimFrame + 1}/{anim.Animations.AnimationDict[int.Parse(SelectedItem.Tag.ToString().Replace("Animation", ""))].DirectionDict[0].Frames.Count}";
                        }
                        catch (Exception exc) { Logger.Log(exc); }
                        try
                        {
                            ObjectInfoText.Content =
                                $"Name: {ItemParent.Header}\n" +
                                $"Type: Active\n" +
                                $"Size: {bmp.Width}x{bmp.Height}\n" +
                                $"Animations: {anim.Animations.AnimationDict.Count}";
                        }
                        catch (Exception exc) { Logger.Log(exc); }
                    }
                    return;
                }
                if (currentReader.getGameData().frameitems[int.Parse(SelectedItem.Tag.ToString().Replace("Object", ""))].properties is ObjectCommon common)
                {
                    if (common.Identifier == "SPRI" || common.Identifier == "SP" || !Settings.twofiveplus && common.Parent.ObjectType == 2)
                    {
                        if (common.Animations?.AnimationDict[0] == null) return;
                        if (common.Animations?.AnimationDict[0].DirectionDict[0] == null) return;
                        if (common.Animations?.AnimationDict[0].DirectionDict[0].Frames[0] == null) return;
                        curAnimFrame++;
                        if (curAnimFrame > common.Animations.AnimationDict[0].DirectionDict[0].Frames.Count - 1) curAnimFrame = 0;
                        var frm = common.Animations.AnimationDict[0].DirectionDict[0].Frames[curAnimFrame];
                        try
                        {
                            var handle = currentReader.getGameData().Images.Items[frm].bitmap.GetHbitmap();
                            ObjectPicture.Source = Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                            UpdateImagePreview();
                            AnimationCurrentFrame.Content = $"{curAnimFrame + 1}/{common.Animations.AnimationDict[0].DirectionDict[0].Frames.Count}";
                        }
                        catch (Exception exc) { Logger.Log(exc); }
                    }
                    else if (common.Identifier == "CNTR" || common.Identifier == "CN" || !Settings.twofiveplus && common.Parent.ObjectType == 7)
                    {
                        var counter = common.Counters;
                        if (counter == null) return;
                        if (!(counter.DisplayType == 1 || counter.DisplayType == 4 || counter.DisplayType == 50)) return;
                        if (counter.Frames == null) return;
                        curAnimFrame++;
                        if (curAnimFrame > counter.Frames.Count - 1) curAnimFrame = 0;
                        var frm = counter.Frames;
                        try
                        {
                            var handle = currentReader.getGameData().Images.Items[frm[curAnimFrame]].bitmap.GetHbitmap();
                            ObjectPicture.Source = Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                            UpdateImagePreview();
                            AnimationCurrentFrame.Content = $"{curAnimFrame + 1}/{counter.Frames.Count}";
                        }
                        catch (Exception exc) { Logger.Log(exc); }
                    }
                }
            }
        }

        private void OpenDumpFolder(object sender, RoutedEventArgs e)
        {
            Directory.CreateDirectory("Dumps\\" + currentReader.getGameData().name);
            Process.Start("explorer.exe", Directory.GetCurrentDirectory() + "\\Dumps\\" + currentReader.getGameData().name + "\\");
        }

        private void ActivateButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void UpdateImagePreview()
        {
            RenderOptions.SetBitmapScalingMode(ObjectPicture, BitmapScalingMode.NearestNeighbor);
            if (ObjectPicture.Source.Width > ObjectPicture.Width || ObjectPicture.Source.Height > ObjectPicture.Height)
                ObjectPicture.Stretch = Stretch.Uniform;
            else
                ObjectPicture.Stretch = Stretch.None;
        }
    }
}
