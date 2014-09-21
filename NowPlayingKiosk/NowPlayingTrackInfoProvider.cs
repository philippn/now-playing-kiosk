using System;

namespace NowPlayingKiosk
{
    public class TrackInfo
    {
        public string Artist { get; set; }
        public string Title { get; set; }

        public TrackInfo(string artist, string title)
        {
            this.Artist = artist;
            this.Title = title;
        }

        public override bool Equals(Object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;
            TrackInfo o = (TrackInfo) obj;
            // Use Equals to compare instance variables.
            return Artist.Equals(o.Artist) && Title.Equals(o.Title);
        }

        public override int GetHashCode()
        {
            return Artist.GetHashCode() ^ Title.GetHashCode();
        }
    }

    public interface NowPlayingTrackInfoProvider
    {
        TrackInfo WhatIsNowPlaying();
    }
}
