using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;
using System;

namespace NowPlayingKiosk
{
    public class SpotifyNowPlayingTrackInfoProvider : NowPlayingTrackInfoProvider
    {
        private readonly SpotifyWebAPI webApi;
        private readonly AuthorizationCodeAuth authorization;
        private Token accessToken;
        private Token refreshToken;

        public SpotifyNowPlayingTrackInfoProvider(SpotifyWebAPI api, AuthorizationCodeAuth auth, Token token)
        {
            webApi = api;
            authorization = auth;
            accessToken = token;
            refreshToken = token;
        }

        public TrackInfo WhatIsNowPlaying()
        {
            DateTime expireDate = accessToken.CreateDate.AddSeconds(accessToken.ExpiresIn);
            if (DateTime.Compare(DateTime.Now, expireDate.AddMinutes(-1)) > 0)
            {
                accessToken = authorization.RefreshToken(refreshToken.RefreshToken).Result;
                webApi.AccessToken = accessToken.AccessToken;
            }

            PlaybackContext context = webApi.GetPlayingTrack();

            if (context.IsPlaying && !TrackType.Ad.Equals(context.CurrentlyPlayingType))
            {
                FullTrack track = context.Item;
                return new TrackInfo(track.Artists[0].Name, track.Name, track.Album.Images[0].Url);
            }
            return null;
        }
    }
}
