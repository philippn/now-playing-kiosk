using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NowPlayingKiosk
{
    public class SpotifyNowPlayingTrackInfoProvider : NowPlayingTrackInfoProvider
    {
        public TrackInfo WhatIsNowPlaying()
        {
            // Get the process
            Process[] processList = Process.GetProcessesByName("spotify");

            string windowTitle = null;
            string wantedPrefix = "Spotify - ";
            foreach (Process process in processList)
            {
                if (!String.IsNullOrEmpty(process.MainWindowTitle) && process.MainWindowTitle.StartsWith(wantedPrefix))
                {
                    windowTitle = process.MainWindowTitle.Substring(wantedPrefix.Length);
                }
            }

            TrackInfo nowPlaying = null;
            if (windowTitle != null)
            {
                // For example "Foo Fighters – Monkey Wrench"
                int Idx = windowTitle.IndexOf('–');
                if (Idx != -1)
                {
                    string artist = windowTitle.Substring(0, Idx).Trim();
                    string title = windowTitle.Substring(Idx + 1).Trim();
                    nowPlaying = new TrackInfo(artist, title);
                }
            }

            return nowPlaying;
        }
    }
}
