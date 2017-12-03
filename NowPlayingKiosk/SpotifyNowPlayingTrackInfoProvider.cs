using SpotifyAPI.Local;
using SpotifyAPI.Local.Enums;
using SpotifyAPI.Local.Models;
using System;

namespace NowPlayingKiosk
{
    public class SpotifyNowPlayingTrackInfoProvider : NowPlayingTrackInfoProvider
    {
        private readonly SpotifyLocalAPI clientApi;
        private Boolean connected;

        public SpotifyNowPlayingTrackInfoProvider()
        {
            clientApi = new SpotifyLocalAPI();
        }

        public TrackInfo WhatIsNowPlaying()
        {
            if (!connected && clientApi.Connect())
            {
                connected = true;
            }
            else if (!connected)
            {
                return null;
            }

            StatusResponse status = clientApi.GetStatus();
            if (status == null)
            {
                connected = false;
                return null;
            }

            if (status.Playing && !status.Track.TrackType.Equals("ad"))
            {
                Track track = status.Track;
                return new TrackInfo(track.ArtistResource.Name, track.TrackResource.Name, track.GetAlbumArtUrl(AlbumArtSize.Size640));
            }
            return null;
        }
    }
}
