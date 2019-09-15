using EasyHttpClient;
using EasyHttpClient.Attributes;
using EasyHttpClient.Attributes.Parameter;
using EasyHttpClient.OAuth2;
using Newtonsoft.Json;
using PivixDownloader.ApiClient.Common;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace PivixDownloader.ApiClient.OAuth
{
    public class PivixOAuthHandler : IOAuth2ClientHandler
    {
        public class PivixOAuthProtocal<TR>
        {
            [JsonProperty("response")]
            public TR Response { get; set; }
        }

        public class PivixOAuthResponse
        {
            [JsonProperty("access_token")]
            public string AccessToken { get; set; }
            [JsonProperty("refresh_token")]
            public string RefreshToken { get; set; }
        }

        public class PivixOAuthRequest
        {
            [HttpAlias("client_id")]
            public string ClientId { get; set; }
            [HttpAlias("client_secret")]
            public string ClientSecret { get; set; }
            [HttpAlias("username")]
            public string Username { get; set; }
            [HttpAlias("password")]
            public string Password { get; set; }
            [HttpAlias("device_token")]
            public string DeviceToken { get; set; }

            [HttpAlias("get_secure_url")]
            public string GetSecureUrl { get; set; }

            [HttpIgnore]
            [HttpAlias("grant_type")]
            public string GrantType { get; set; }
        }

        public interface IOAuth2Api
        {
            [Route("{oAuth2TokenPath}")]
            [HttpPost]
            Task<IHttpResult<PivixOAuthProtocal<PivixOAuthResponse>>> AuthToken([PathParam]string oAuth2TokenPath, [FormBody]PivixOAuthRequest request, [FormBody]string grant_type);

            [Route("{oAuth2TokenPath}")]
            [HttpPost]
            Task<IHttpResult<PivixOAuthProtocal<PivixOAuthResponse>>> RefreshToken([PathParam]string oAuth2TokenPath, [FormBody]PivixOAuthRequest request, [FormBody]string refresh_token, [FormBody]string grant_type);
        }

        private readonly IOAuth2Api _oAuth2Api;
        private readonly string _oAuth2TokenPath;
        private Task<IHttpResult<PivixOAuthProtocal<PivixOAuthResponse>>> authTokenTask;
        private Task<IHttpResult<PivixOAuthProtocal<PivixOAuthResponse>>> refreshTokenTask;
        private PivixOAuthResponse oAuthResponse;

        private readonly PivixOAuthRequest  _request;
        public PivixOAuthHandler(string loginHost, PivixOAuthRequest request)
            : this(loginHost, "auth/token", request)
        {

        }
        public PivixOAuthHandler(string loginHost, string oAuth2TokenPath, PivixOAuthRequest request)
        {
            this._request = request;
            this._oAuth2TokenPath = oAuth2TokenPath.Trim('/');
            var factory = new EasyHttpClientFactory();
            factory.Config.HttpClientProvider = new PivixHttpClientProvier();
            _oAuth2Api = factory.Create<IOAuth2Api>(loginHost);
        }

        public async Task SetAccessToken(HttpRequestMessage originalHttpRequestMessage)
        {
            if (oAuthResponse == null)
            {
                var result = await TaskWhenEnd(authTokenTask, () => _oAuth2Api.AuthToken(this._oAuth2TokenPath, this._request, this._request.GrantType));

                if (result != null && result.IsSuccessStatusCode)
                {
                    oAuthResponse = result.Content?.Response;
                }
            }

            if (oAuthResponse != null)
            {
                originalHttpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", oAuthResponse.AccessToken); ;
            }
        }

        public async Task<bool> RefreshAccessToken(HttpRequestMessage originalHttpRequestMessage)
        {
            var canRefreshToken = oAuthResponse != null && !string.IsNullOrWhiteSpace(oAuthResponse.RefreshToken);
            IHttpResult<PivixOAuthProtocal<PivixOAuthResponse>> result = null;
            if (canRefreshToken)
            {
                result = await TaskWhenEnd(refreshTokenTask, () => _oAuth2Api.RefreshToken(this._oAuth2TokenPath, this._request, oAuthResponse.RefreshToken, "refresh_token"));

                if (result != null && result.IsSuccessStatusCode)
                {
                    oAuthResponse = result.Content.Response;
                }
            }
            if (!canRefreshToken || (result != null && result.StatusCode == HttpStatusCode.Unauthorized))
            {
                result = await TaskWhenEnd(authTokenTask, () => _oAuth2Api.AuthToken(this._oAuth2TokenPath, this._request, this._request.GrantType));

                if (result != null && result.IsSuccessStatusCode)
                {
                    oAuthResponse = result.Content.Response;
                }
            }
            if (oAuthResponse != null)
            {
                originalHttpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", oAuthResponse.AccessToken);
                return true;
            }
            else
            {
                return false;
            }
        }

        private  Task<T> TaskWhenEnd<T>(Task<T> task,Func<Task<T>> valueFactory)
        {
            if (task == null || task.IsCanceled || task.IsCompleted || task.IsFaulted)
            {
                task = valueFactory();
            }
            return task;
        }
    }
}
