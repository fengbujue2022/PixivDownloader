using System;

namespace PivixDownloader
{
    class Program
    {
        static void Main(string[] args)
        {
            var simpleClient = SimpleHttpClient.HttpClientFactory.Create((handler) =>
            {
                handler.EndPointProvider = new PivixEndPointProvider();
            }, new PivixHeaderValueHandler());
            var response = await simpleClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://i.pximg.net/img-master/img/2017/07/08/22/38/22/63771031_p0_master1200.jpg"));

            using (var s = File.Open($@"C:\Users\windows\Desktop\63771031_p0_master1200.jpg", FileMode.OpenOrCreate))
            {
                await response.Content.CopyToAsync(s);
            }
        }
    }

    public class PivixHeaderValueHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            request.Headers.Add("User-Agent", "PixivIOSApp/5.8.0");
            request.Headers.Add("referer", "https://app-api.pixiv.net/");
            return base.SendAsync(request, cancellationToken);
        }
    }

    public class PivixEndPointProvider : EndPointProvider
    {
        private readonly IDictionary<string, string> DNSMap = new Dictionary<string, string>()
        {
            {"i.pximg.net","210.140.92.136"},
        };

        public override EndPoint GetEndPoint(string host, int port)
        {
            if (DNSMap.Keys.Contains(host))
                return new IPEndPoint(IPAddress.Parse(DNSMap[host]), port);

            return base.GetEndPoint(host, port);
        }
    }
}
