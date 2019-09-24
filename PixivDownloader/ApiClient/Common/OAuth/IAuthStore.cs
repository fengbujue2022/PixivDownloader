using Newtonsoft.Json;
using PixivDownloader.ApiClient.OAuth;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace PixivDownloader.ApiClient.Common.OAuth
{
    public interface IAuthStore
    {
        Task AddAuthResponseAsync(PixivOAuthResponse pixivOAuth);
        Task<PixivOAuthResponse> GetAuthResponseAsync();
    }

    public class TextFileAuthStore : IAuthStore
    {
        private readonly string _storageFileName = "oauthInfo.json";

        public async Task AddAuthResponseAsync(PixivOAuthResponse pixivOAuth)
        {
            var json = JsonConvert.SerializeObject(pixivOAuth);
            var buffer = Encoding.UTF8.GetBytes(json);
            var path = GetPathWithBaseDirectory();
            using (var fileStream = new FileStream(GetPathWithBaseDirectory(), FileMode.OpenOrCreate))
            {
                await fileStream.WriteAsync(buffer);
            }
        }

        public async Task<PixivOAuthResponse> GetAuthResponseAsync()
        {
            var path = GetPathWithBaseDirectory();
            if (!File.Exists(path))
            {
                return null;
            }

            using (var fileStream = new FileStream(GetPathWithBaseDirectory(), FileMode.Open))
            {
                var buffer = new byte[fileStream.Length];
                await fileStream.ReadAsync(buffer, 0, buffer.Length);
                var result = JsonConvert.DeserializeObject<PixivOAuthResponse>(Encoding.UTF8.GetString(buffer));
                return result;
            }
        }

        private string GetPathWithBaseDirectory() => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _storageFileName);

    }
}
