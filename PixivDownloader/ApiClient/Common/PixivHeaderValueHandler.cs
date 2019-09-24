using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace PixivDownloader.ApiClient.Common
{
    public class PixivHeaderValueHandler : System.Net.Http.DelegatingHandler
    {
        private readonly string hashSecret = "28c1fdd170a5204386cb1313c7077b34f83e4aaf4aa829ce78c231e05b0bae2c";

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            string time = DateTime.UtcNow.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            request.Headers.Add("X-Client-Time", time);
            request.Headers.Add("X-Client-Hash", MD5Hash(time + hashSecret));
            request.Headers.Add("User-Agent", "PixivIOSApp/5.8.0");
            request.Headers.Add("Accept-Language", "zh-CN");
            request.Headers.Add("Referer", "https://app-api.pixiv.net/");
            return base.SendAsync(request, cancellationToken);

            string MD5Hash(string Input)
            {
                using (var md5 = MD5.Create())
                {
                    var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(Input.Trim()));
                    StringBuilder builder = new StringBuilder();
                    for (int i = 0; i < bytes.Length; i++)
                        builder.Append(bytes[i].ToString("x2"));
                    return builder.ToString();
                }
            }
        }
    }
}
