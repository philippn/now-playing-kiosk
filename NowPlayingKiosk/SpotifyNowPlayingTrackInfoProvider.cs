using SpotifyAPI.Web;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;
using System;

namespace NowPlayingKiosk
{
    public class SpotifyNowPlayingTrackInfoProvider : NowPlayingTrackInfoProvider
    {
        private readonly SpotifyWebAPI clientApi;

        public SpotifyNowPlayingTrackInfoProvider(SpotifyWebAPI api)
        {
            clientApi = api;
        }

        public TrackInfo WhatIsNowPlaying()
        {
            PlaybackContext context = clientApi.GetPlayingTrack();

            if (context.IsPlaying && !TrackType.Ad.Equals(context.CurrentlyPlayingType))
            {
                FullTrack track = context.Item;
                return new TrackInfo(track.Artists[0].Name, track.Name, track.Album.Images[0].Url);
            }
            return null;
        }
    }
}
