using PivixDownloader.ApiClient;
using PivixDownloader.ApiClient.Api;
using SimpleHttpClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace PivixDownloader
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var factory = new PivixApiClientFactory();
            var pivixApiClient = factory.Create<IPivixApiClient>();

            //TODO 吃饭要紧 溜了溜了
        }
    }

}
