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
            else if (info.AlbumArtUrl != null)
            {
                DownloadImage(info.AlbumArtUrl, file);
                return file;
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

        private void DownloadImage(string url, FileInfo file)
        {
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
