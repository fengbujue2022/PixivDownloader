using SimpleHttpClient;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace PixivDownloader.ApiClient.Common
{
    public class PixivEndPointProvider : EndPointProvider
    {
        private readonly IDictionary<string, string> DNSMap = new Dictionary<string, string>()
        {
            { "i.pximg.net","210.140.92.136"},
            { "app-api.pixiv.net","210.140.131.224"},
            { "accounts.pixiv.net","210.140.131.222"},
            { "oauth.secure.pixiv.net","210.140.131.219"},
        };

        public override EndPoint GetEndPoint(string host, int port)
        {
            if (DNSMap.Keys.Contains(host))
                return new IPEndPoint(IPAddress.Parse(DNSMap[host]), port);
            return base.GetEndPoint(host, port);
        }

        public override string GetHost(string host)
        {
            if (DNSMap.Keys.Contains(host))
                return DNSMap[host];

            return base.GetHost(host);
        }
    }

    /*
     DNS query
     requset
     https://1.0.0.1/dns-query?ct=application/dns-json&name=i.pximg.net&type=A&do=false&cd=false
     response
     {
    "Status": 0,
    "TC": false,
    "RD": true,
    "RA": true,
    "AD": false,
    "CD": false,
    "Question": [
        {
            "name": "i.pximg.net.",
            "type": 1
        }
    ],
    "Answer": [
        {
            "name": "i.pximg.net.",
            "type": 1,
            "TTL": 187,
            "data": "210.140.92.135"
        },
        {
            "name": "i.pximg.net.",
            "type": 1,
            "TTL": 187,
            "data": "210.140.92.136"
        },
        {
            "name": "i.pximg.net.",
            "type": 1,
            "TTL": 187,
            "data": "210.140.92.137"
        },
        {
            "name": "i.pximg.net.",
            "type": 1,
            "TTL": 187,
            "data": "210.140.92.138"
        },
        {
            "name": "i.pximg.net.",
            "type": 1,
            "TTL": 187,
            "data": "210.140.92.139"
        },
        {
            "name": "i.pximg.net.",
            "type": 1,
            "TTL": 187,
            "data": "210.140.92.140"
        },
        {
            "name": "i.pximg.net.",
            "type": 1,
            "TTL": 187,
            "data": "210.140.92.141"
        },
        {
            "name": "i.pximg.net.",
            "type": 1,
            "TTL": 187,
            "data": "210.140.92.142"
        },
        {
            "name": "i.pximg.net.",
            "type": 1,
            "TTL": 187,
            "data": "210.140.92.143"
        },
        {
            "name": "i.pximg.net.",
            "type": 1,
            "TTL": 187,
            "data": "210.140.92.144"
        }
    ]
}
     */
}
