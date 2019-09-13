using SimpleHttpClient;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace PivixDownloader.ApiClient.Common
{
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
