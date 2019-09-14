using EasyHttpClient;
using PivixDownloader.ApiClient.Common;
using PivixDownloader.ApiClient.OAuth;
using System;
using System.Collections.Generic;
using System.Text;

namespace PivixDownloader.ApiClient
{
    public class PivixApiClientFactory : EasyHttpClientFactory
    {
        public PivixApiClientFactory() : base()
        {
            this.Config.Host = new Uri("https://app-api.pixiv.net");
            this.Config.HttpClientProvider = new PivixHttpClientProvier();
            this.Config.HttpClientSettings.OAuth2ClientHandler = new PivixOAuthHandler(
                "https://oauth.secure.pixiv.net/",
                new PivixOAuthHandler.PivixOAuthRequest
                {
                    ClientId = "MOBrBDS8blbauoSck0ZfDbtuzpyT",
                    ClientSecret = "lsACyCD94FhDUtGTXi3QzcFE2uU1hqtDaKeqrdwj",
                    GrantType = "password",
                    //DeviceToken = "6b466a56becc789aa8821ecc8f607d3c",
                    Username = "",
                    Password = "",
                }
            );
        }
    }
}
