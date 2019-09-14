using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PivixDownloader.ApiClient.Common
{
    public class PivixHeaderValueHandler : System.Net.Http.DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            request.Headers.Add("User-Agent", "PixivIOSApp/5.8.0");
            request.Headers.Add("Accept-Language", "zh-CN");
            request.Headers.Add("referer", "https://app-api.pixiv.net/");
            return base.SendAsync(request, cancellationToken);
        }
    }
}
