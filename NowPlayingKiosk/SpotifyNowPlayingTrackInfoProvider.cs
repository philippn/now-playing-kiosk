using System;
using System.Diagnostics;

namespace NowPlayingKiosk
{
    public class SpotifyNowPlayingTrackInfoProvider : NowPlayingTrackInfoProvider
    {
        private TrackInfo lastPlayed;

        public TrackInfo WhatIsNowPlaying()
        {
            // Get the process
            Process[] processList = Process.GetProcessesByName("spotify");

            string wantedPrefix = "Spotify - ";
            foreach (Process process in processList)
            {
                if (!String.IsNullOrEmpty(process.MainWindowTitle) && process.MainWindowTitle.Equals("Spotify"))
                {
                    // Nothing playing right now
                    lastPlayed = null;
                    return null;
                }
                else if (!String.IsNullOrEmpty(process.MainWindowTitle) && process.MainWindowTitle.StartsWith(wantedPrefix))
                {
                    // Spotify is playing something
                    string windowTitle = process.MainWindowTitle.Substring(wantedPrefix.Length);
                    TrackInfo nowPlaying = null;
                    if (windowTitle != null)
                    {
                        // For example "Foo Fighters – Monkey Wrench"
                        int idx = windowTitle.IndexOf('–');
                        if (idx != -1)
                        {
                            string artist = windowTitle.Substring(0, idx).Trim();
                            string title = windowTitle.Substring(idx + 1).Trim();
                            nowPlaying = new TrackInfo(artist, title);
                        }
                    }

                    lastPlayed = nowPlaying;
                    return nowPlaying;
                }
                else if (String.IsNullOrEmpty(process.MainWindowTitle))
                {
                    return lastPlayed;
                }
            }

            return null;
        }
    }
}
