using EasyHttpClient;
using EasyHttpClient.Attributes;
using EasyHttpClient.OAuth2;
using Newtonsoft.Json;
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

        public class PivixOAuthResponse
        {
            [JsonProperty("access_token")]
            public string AccessToken { get; set; }
            [JsonProperty("refresh_token")]
            public string RefreshToken { get; set; }
        }

        public class PivixOAuthRequest
        {
            [JsonProperty("client_id")]
            public string ClientId { get; set; }
            [JsonProperty("client_secret")]
            public string ClientSecret { get; set; }
            [JsonProperty("Username")]
            public string Username { get; set; }
            [JsonProperty("password")]
            public string Password { get; set; }
            [JsonProperty("device_token")]
            public string DeviceToken { get; set; }

            [JsonIgnore]
            [JsonProperty("grant_type")]
            public string GrantType { get; set; }
        }

        public interface IOAuth2Api
        {
            [Route("{oAuth2TokenPath}")]
            [HttpPost]
            Task<IHttpResult<PivixOAuthResponse>> AuthToken([PathParam]string oAuth2TokenPath, [FormBody]PivixOAuthRequest request, [FormBody]string grant_type);

            [Route("{oAuth2TokenPath}")]
            [HttpPost]
            Task<IHttpResult<PivixOAuthResponse>> RefreshToken([PathParam]string oAuth2TokenPath, [FormBody]PivixOAuthRequest request, [FormBody]string refresh_token, [FormBody]string grant_type);
        }

        private readonly IOAuth2Api _oAuth2Api;
        private readonly string _oAuth2TokenPath;
        private Task<IHttpResult<PivixOAuthResponse>> authTokenTask;
        private Task<IHttpResult<PivixOAuthResponse>> refreshTokenTask;
        private PivixOAuthResponse oAuthResponse;

        private readonly PivixOAuthRequest  _request;
        public PivixOAuthHandler(string loginHost, PivixOAuthRequest request)
            : this(loginHost, "oauth2/token", request)
        {

        }
        public PivixOAuthHandler(string loginHost, string oAuth2TokenPath, PivixOAuthRequest request)
        {
            this._request = request;
            this._oAuth2TokenPath = oAuth2TokenPath.Trim('/');
            var factory = new EasyHttpClientFactory();
            _oAuth2Api = factory.Create<IOAuth2Api>(loginHost);
        }

        public async Task SetAccessToken(HttpRequestMessage originalHttpRequestMessage)
        {
            if (oAuthResponse == null)
            {
                if (authTokenTask == null
                     || authTokenTask.IsCanceled
                     || authTokenTask.IsCompleted
                     || authTokenTask.IsFaulted)
                {
                    authTokenTask = _oAuth2Api.AuthToken(this._oAuth2TokenPath, this._request, this._request.GrantType);
                }
                var result = await authTokenTask;
                if (result != null && result.IsSuccessStatusCode)
                {
                    oAuthResponse = result.Content;
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
            IHttpResult<PivixOAuthResponse> result = null;
            if (canRefreshToken)
            {
                if (refreshTokenTask == null
                     || refreshTokenTask.IsCanceled
                     || refreshTokenTask.IsCompleted
                     || refreshTokenTask.IsFaulted)
                {
                    refreshTokenTask = _oAuth2Api.RefreshToken(this._oAuth2TokenPath, this._request, oAuthResponse.RefreshToken, "refresh_token");
                }
                result = await refreshTokenTask;

                if (result != null && result.IsSuccessStatusCode)
                {
                    oAuthResponse = result.Content;
                }
            }
            if (!canRefreshToken || (result != null && result.StatusCode == HttpStatusCode.Unauthorized))
            {
                if (authTokenTask == null
                     || authTokenTask.IsCanceled
                     || authTokenTask.IsCompleted
                     || authTokenTask.IsFaulted)
                {
                    authTokenTask = _oAuth2Api.AuthToken(this._oAuth2TokenPath, this._request, this._request.GrantType);
                }
                result = await authTokenTask;
                if (result != null && result.IsSuccessStatusCode)
                {
                    oAuthResponse = result.Content;
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
    }
}
