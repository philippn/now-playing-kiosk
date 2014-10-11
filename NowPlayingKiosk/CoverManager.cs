using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;

namespace NowPlayingKiosk
{
    [DataContract]
    public class Response
    {
        [DataMember(Name = "tracks")]
        public Resource Tracks { get; set; }
    }

    [DataContract]
    public class Resource
    {
        [DataMember(Name = "href")]
        public string Href { get; set; }
        [DataMember(Name = "items")]
        public Item[] Items { get; set; }
    }

    [DataContract]
    public class Item
    {
        [DataMember(Name = "album")]
        public Album Album { get; set; }
    }

    [DataContract]
    public class Album
    {
        [DataMember(Name = "album_type")]
        public string AlbumType { get; set; }
        [DataMember(Name = "available_markets")]
        public string[] AvailableMarkets { get; set; }
        [DataMember(Name = "images")]
        public Image[] Images { get; set; }
    }

    [DataContract]
    public class Image
    {
        [DataMember(Name = "height")]
        public int Height { get; set; }
        [DataMember(Name = "width")]
        public int Width { get; set; }
        [DataMember(Name = "url")]
        public string Url { get; set; }
    }

    public class CoverManager
    {
        private MD5 Md5Hash = MD5.Create();

        public FileInfo GetCover(TrackInfo info)
        {
            string hash = GetMd5Hash(info.Artist + ":" + info.Title);
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "NowPlayingKiosk", "Cache", hash.Substring(0, 2));
            Directory.CreateDirectory(path);

            string filename = Path.Combine(path, hash + ".png");
            FileInfo file = new FileInfo(filename);
            if (file.Exists)
            {
                // Get from cache
                return file;
            }
            else
            {
                Response resp = MakeRequest(info);
                if (resp != null)
                {
                    Item item = ProcessResponse(resp);
                    if (item != null)
                    {
                        DownloadImage(item, file);
                        return file;
                    }
                }
            }

            return GetDefaultCover();
        }

        public FileInfo GetDefaultCover()
        {
            // Find default Cover image (if any)
            string skinDir = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Skin");
            string[] coverImages = Directory.GetFiles(@skinDir, "cover.*");
            if (coverImages.Length > 0)
            {
                return new FileInfo(coverImages[0]);
            }

            return null;
        }

        private string GetMd5Hash(string input)
        {
            // Convert the input string to a byte array and compute the hash.
            byte[] data = Md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data 
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }

        private Response MakeRequest(TrackInfo info)
        {
            try
            {
                string query = "q=artist:" + WebUtility.UrlEncode(info.Artist) + "+track:" + WebUtility.UrlEncode(info.Title);
                string market = CultureInfo.InstalledUICulture.TwoLetterISOLanguageName;
                string url = "https://api.spotify.com/v1/search?" + query + "&type=track&market=" + market;
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                        throw new Exception(String.Format(
                        "Server error (HTTP {0}: {1}).",
                        response.StatusCode,
                        response.StatusDescription));
                    DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(Response));
                    object objResponse = jsonSerializer.ReadObject(response.GetResponseStream());
                    Response jsonResponse
                    = objResponse as Response;
                    return jsonResponse;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }

        private Item ProcessResponse(Response response)
        {
            foreach (Item item in response.Tracks.Items)
            {
                // Prefer albums
                if (IsOfType(item, "album"))
                {
                    return item;
                }
            }

            if (response.Tracks.Items.Length > 0)
            {
                return response.Tracks.Items[0];
            }

            return null;
        }

        private bool IsOfType(Item item, string albumType)
        {
            return item.Album.AlbumType.Equals(albumType);
        }

        private void DownloadImage(Item item, FileInfo file)
        {
            string url = item.Album.Images[0].Url;
            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
            {
                if (response.StatusCode != HttpStatusCode.OK)
                    throw new Exception(String.Format(
                    "Server error (HTTP {0}: {1}).",
                    response.StatusCode,
                    response.StatusDescription));
                using (var fileStream = file.Create())
                {
                    response.GetResponseStream().CopyTo(fileStream);
                }
            }
        }
    }
}
