using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;
using System;
using System.ComponentModel;
using System.Configuration;
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
                    if (!nowPlaying.Equals(lastPlayed) || (lastPlayed.AlbumArtUrl == null))
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

    public class RelayCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;
        private readonly Action Action;

        public RelayCommand(Action action)
        {
            Action = action;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            Action();
        }
    }

    public class ViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string Artist { get; set; }
        public string Title { get; set; }
        public string CoverImage { get; set; }
        public string BackgroundImage { get; set; }
        public ICommand LoginCommand { get; set; }

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
        private SpotifyWebAPI webApi;

        public MainWindow()
        {
            InitializeComponent();

            // These settings are located in App.config file
            string _clientId = ConfigurationManager.AppSettings["ClientId"].ToString();
            string _clientSecret = ConfigurationManager.AppSettings["ClientSecret"].ToString();

            AuthorizationCodeAuth auth =
                new AuthorizationCodeAuth(_clientId, _clientSecret, "http://127.0.0.1:4002", "http://127.0.0.1:4002",
                    Scope.UserReadCurrentlyPlaying);
            auth.AuthReceived += LoggedInSuccessfully;
            auth.Start();

            ViewModel viewModel = new ViewModel
            {
                LoginCommand = new RelayCommand(auth.OpenBrowser)
            };

            DataContext = viewModel;
            Content = LoadSkin("LoginPage.xaml");
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
            if (poller != null)
            {
                poller.StopAlready = true;
            }
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

        private Page LoadSkin(string template)
        {
            string skinPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Skin", template);
            FileStream fs = new FileStream(@skinPath, FileMode.Open);
            return System.Windows.Markup.XamlReader.Load(fs) as Page;
        }

        private void GoToMainPage()
        {
            Page mainPage = LoadSkin("MainPage.xaml");
            Content = mainPage;
        }

        private async void LoggedInSuccessfully(object sender, AuthorizationCode payload)
        {
            AuthorizationCodeAuth auth = (AuthorizationCodeAuth)sender;
            auth.Stop();

            Dispatcher.Invoke(DispatcherPriority.Render, new Action(() => GoToMainPage()));

            Token token = await auth.ExchangeCode(payload.Code);
            webApi = new SpotifyWebAPI
            {
                AccessToken = token.AccessToken,
                TokenType = token.TokenType
            };

            StartPoller(auth, token);
        }

        private void StartPoller(AuthorizationCodeAuth auth, Token token)
        {
            CoverManager coverManager = new CoverManager();
            NowPlayingTrackInfoProvider trackInfoProvider = new SpotifyNowPlayingTrackInfoProvider(webApi, auth, token);
            poller = new TrackInfoPoller(trackInfoProvider, coverManager, this);
            Thread pollerThread = new Thread(new ThreadStart(poller.PollAndUpdate));
            pollerThread.Start();
        }
    }
}
