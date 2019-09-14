using PivixDownloader.ApiClient;
using PivixDownloader.ApiClient.Api;
using SimpleHttpClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Management;
using System.Linq;

namespace PivixDownloader
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var factory = new PivixApiClientFactory("username","password");
            var pivixApiClient = factory.Create<IPivixApiClient>();

            var result = await pivixApiClient.SearchIllust("珂朵莉");
            
        }
    }

}
