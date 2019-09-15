﻿using PivixDownloader.ApiClient;
using PivixDownloader.ApiClient.Api;
using SimpleHttpClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using PivixDownloader.ApiClient.Common;
using System.Threading;

namespace PivixDownloader
{
    class Program
    {
        //configurable for yourself
        private static string Username = "";
        private static string Password = "";
        private static string DownloadDir = @"C:\Pivix";


        public static Lazy<PivixHttpClientProvier> pivixHttpClientProvier = new Lazy<PivixHttpClientProvier>();

        static async Task Main(string[] args)
        {
            var factory = new PivixApiClientFactory(Username, Password);
            var pivixApiClient = factory.Create<IPivixApiClient>();

            var searchResponse = await pivixApiClient.SearchIllust("珂朵莉");

            searchResponse.illusts = searchResponse.illusts.OrderByDescending(x => x.total_bookmarks).ToList();
            var theFirstOneId = searchResponse.illusts.First().id;

            var relatedResponse = await pivixApiClient.IllustRelated(theFirstOneId);

            foreach (var illust in relatedResponse.illusts)
            {
                await Download(illust.image_urls.large);
            }
        }


        private static async Task Download(string url)
        {
            var client = pivixHttpClientProvier.Value.GetClient(null);

            if (!Directory.Exists(DownloadDir))
                Directory.CreateDirectory(DownloadDir);

            var fileName = ResolveFileName();
            var finalPath = Path.Combine(DownloadDir, fileName);

            if (!File.Exists(finalPath))
            {
                var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, url), CancellationToken.None);
                if (response.Content.Headers.ContentType!=null && response.Content.Headers.ContentType.MediaType.Contains("image"))
                {
                    using (var fs = new FileStream(finalPath, FileMode.CreateNew))
                    {
                        await response.Content.CopyToAsync(fs);
                    }
                }
            }

            string ResolveFileName()
            {
                return url.Split('/').Last(); ;
            }
        }
    }

}
