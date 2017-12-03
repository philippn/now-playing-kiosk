using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace NowPlayingKiosk
{
    public class TrackInfoPoller
    {
        public volatile bool StopAlready;

        private NowPlayingTrackInfoProvider nowPlayingProvider;
        private CoverManager coverManager;
        private Window window;
        private Dispatcher dispatcher;

        public TrackInfoPoller(NowPlayingTrackInfoProvider Provider, CoverManager CoverManager, Window window)
        {
            this.nowPlayingProvider = Provider;
            this.coverManager = CoverManager;
            this.window = window;
            this.dispatcher = App.Current.Dispatcher;
        }

        public void PollAndUpdate()
        {
            // Find background image (if any)
            string skinDir = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Skin");
            string[] backgroundImages = Directory.GetFiles(@skinDir, "background.*");

            TrackInfo lastPlayed = null;

            while (!StopAlready)
            {
                TrackInfo nowPlaying = nowPlayingProvider.WhatIsNowPlaying();
                if (nowPlaying != null)
                {
                    if (!nowPlaying.Equals(lastPlayed))
                    {
                        ViewModel model = new ViewModel();

                        // Fetching the Cover may take some time, so do it first
                        FileInfo coverFile = coverManager.GetCover(nowPlaying);
                        if (coverFile != null)
                        {
                            model.CoverImage = coverFile.FullName;
                        }
                        model.Artist = nowPlaying.Artist;
                        model.Title = nowPlaying.Title;
                        if (backgroundImages.Length > 0)
                        {
                            model.BackgroundImage = backgroundImages[0];
                        }

                        dispatcher.Invoke(DispatcherPriority.Render, new Action(() => RefreshWindow(model)));
                        lastPlayed = nowPlaying;
                    }
                }
                else
                {
                    // Reset model
                    ViewModel model = new ViewModel();

                    FileInfo coverFile = coverManager.GetDefaultCover();
                    if (coverFile != null)
                    {
                        model.CoverImage = coverFile.FullName;
                    }
                    if (backgroundImages.Length > 0)
                    {
                        model.BackgroundImage = backgroundImages[0];
                    }

                    dispatcher.Invoke(DispatcherPriority.Render, new Action(() => RefreshWindow(model)));
                    lastPlayed = null;
                }

                Thread.Sleep(5000);
            }
        }

        private void RefreshWindow(ViewModel model)
        {
            window.DataContext = model;
        }
    }

    public class ViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string Artist { get; set; }
        public string Title { get; set; }
        public string CoverImage { get; set; }
        public string BackgroundImage { get; set; }

        protected void RaisePropertyChangedEvent(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool _inStateChange;
        private TrackInfoPoller poller;

        public MainWindow()
        {
            InitializeComponent();

            LoadSkin();
            StartPoller();
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if ((WindowState == WindowState.Maximized) && !_inStateChange)
            {
                _inStateChange = true;
                WindowState = WindowState.Normal;
                WindowStyle = WindowStyle.None;
                WindowState = WindowState.Maximized;
                ResizeMode = ResizeMode.NoResize;
                _inStateChange = false;
            }
            base.OnStateChanged(e);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            poller.StopAlready = true;
            base.OnClosing(e);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if ((WindowState == WindowState.Maximized) && (e.Key == Key.Escape))
            {
                ResizeMode = ResizeMode.CanResize;
                WindowState = WindowState.Normal;
                WindowStyle = WindowStyle.ThreeDBorderWindow;
            }
        }

        private void LoadSkin()
        {
            string SkinPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Skin", "skin.xaml");
            FileStream fs = new FileStream(@SkinPath, FileMode.Open);
            Grid grid = System.Windows.Markup.XamlReader.Load(fs) as Grid;
            Content = grid;
        }

        private void StartPoller()
        {
            CoverManager coverManager = new CoverManager();
            NowPlayingTrackInfoProvider trackInfoProvider = new SpotifyNowPlayingTrackInfoProvider();
            poller = new TrackInfoPoller(trackInfoProvider, coverManager, this);
            Thread pollerThread = new Thread(new ThreadStart(poller.PollAndUpdate));
            pollerThread.Start();
        }
    }
}
