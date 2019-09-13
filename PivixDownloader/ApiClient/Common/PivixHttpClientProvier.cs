using EasyHttpClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace PivixDownloader.ApiClient.Common
{
    public class PivixHttpClientProvier : IHttpClientProvider
    {
        private object syncObject = new object();
        private HttpMessageInvoker _httpClient;

        public HttpMessageInvoker GetClient(HttpClientSettings clientSetting, params DelegatingHandler[] handlers)
        {
            if (_httpClient == null)
            {
                lock (syncObject)
                {
                    if (_httpClient == null)
                    {
                        _httpClient = GetClientInernal(clientSetting, handlers);
                    }
                }
            }
            return _httpClient;
        }

        public HttpMessageInvoker GetClientInernal(HttpClientSettings clientSetting, params DelegatingHandler[] handlers)
        {
            var defaultHandlers = new DelegatingHandler[] {
                new PivixHeaderValueHandler()
            };
            handlers = handlers == null ? defaultHandlers : handlers.Concat(defaultHandlers).ToArray();

            var httpClient = SimpleHttpClient.HttpClientFactory.Create((handler) =>
            {
                handler.EndPointProvider = new PivixEndPointProvider();
            }, handlers);

            return httpClient;
        }
    }
}
