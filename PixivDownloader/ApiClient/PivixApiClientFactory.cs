using EasyHttpClient;
using PixivDownloader.ApiClient.Common;
using PixivDownloader.ApiClient.Common.OAuth;
using PixivDownloader.ApiClient.OAuth;
using System;
using System.Collections.Generic;
using System.Text;

namespace PixivDownloader.ApiClient
{
    public class PixivApiClientFactory : EasyHttpClientFactory
    {
        public PixivApiClientFactory(string username,string password) : base()
        {
            this.Config.Host = new Uri("https://app-api.pixiv.net");
            this.Config.HttpClientProvider = new PixivHttpClientProvier();
            this.Config.HttpClientSettings.Timeout = TimeSpan.FromSeconds(120);
            this.Config.HttpClientSettings.OAuth2ClientHandler = new PixivOAuthHandler(
                "https://oauth.secure.pixiv.net/",
                new PixivOAuthRequest
                {
                    ClientId = "MOBrBDS8blbauoSck0ZfDbtuzpyT",
                    ClientSecret = "lsACyCD94FhDUtGTXi3QzcFE2uU1hqtDaKeqrdwj",
                    GrantType = "password",
                    GetSecureUrl = "1",
                    Username = username,
                    Password = password,
                },
                new TextFileAuthStore()
            );
        }
    }
}
