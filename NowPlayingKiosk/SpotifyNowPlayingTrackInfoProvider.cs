using SpotifyAPI.Local;
using SpotifyAPI.Local.Enums;
using SpotifyAPI.Local.Models;
using System;
using System.Net;

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
            if (!SpotifyLocalAPI.IsSpotifyRunning() || 
                !SpotifyLocalAPI.IsSpotifyWebHelperRunning())
            {
                // Either the spotify client or WebHelper is not running. Ouch.
                connected = false;
                return null;
            }

            if (!connected && clientApi.Connect())
            {
                connected = true;
            }
            else if (!connected)
            {
                return null;
            }

            StatusResponse status = clientApi.GetStatus(); // GetStatus() is safe to use
            if (status == null)
            {
                connected = false;
                return null;
            }

            if (status.Playing && !status.Track.IsAd())
            {
                Track track = status.Track;
                string albumArtUrl = null;
                try
                {
                    albumArtUrl = track.GetAlbumArtUrl(AlbumArtSize.Size640);
                }
                catch (WebException)
                {
                    // Do nothing
                }
                return new TrackInfo(track.ArtistResource.Name, track.TrackResource.Name, albumArtUrl);
            }
            return null;
        }
    }
}
