using SimpleHttpClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;
using PixivDownloader.ApiClient.Common;
using PixivDownloader.ApiClient;
using PixivDownloader.ApiClient.Api;
using PixivDownloader.ApiClient.Response;

namespace PixivDownloader
{
    class Program
    {
        //configurable for yourself
        private static readonly string Username = "";
        private static readonly string Password = "";
        private static readonly string DownloadDir = @"D:\Pixiv";

        private static Lazy<PixivHttpClientProvier> PixivHttpClientProvier = new Lazy<PixivHttpClientProvier>();
        private static PixivApiClientFactory factory = new PixivApiClientFactory(Username, Password);
        private static IPixivApiClient pixivApiClient = factory.Create<IPixivApiClient>();

        static async Task Main(string[] args)
        {
            var hbSearchResult = await SearchThenTakeHighlyBookmark(
                keyword: "heaven's feel",
                bookmarkLimit: 1000,
                takeCount: 20);

            var relateResult =await GetRelatedWithSearchResult(hbSearchResult);

            ParallelDownload(relateResult.Select(x => x.meta_single_page.original_image_url));
        }

        private static async Task<IEnumerable<Illusts>> SearchThenTakeHighlyBookmark(string keyword, int bookmarkLimit, int takeCount)
        {
            var maxCalledCount = 10;
            var offset = 1;

            var illusts = new List<Illusts>();

            while (maxCalledCount > 0)
            {
                var searchResult = await pixivApiClient.SearchIllust(keyword, offset: offset);
                illusts.AddRange(searchResult.illusts.Where(x => x.total_bookmarks > bookmarkLimit).ToList());
                if (illusts.Count >= takeCount)
                {
                    break;
                }
                maxCalledCount--;
                offset++;
            }
            return illusts.Take(takeCount);
        }

        private static async Task<IEnumerable<Illusts>> GetRelatedWithSearchResult(IEnumerable<Illusts> illusts)
        {
            var result = new List<Illusts>();
            result.AddRange(illusts);

            foreach (var illust in illusts)
            {
                await DoGetRelated(illust.id);
            }

            return result;

            async Task DoGetRelated(int illustId)
            {
                var relateRseult = await pixivApiClient.IllustRelated(illustId);
                if (relateRseult != null)
                {
                    var existingIds = result.Select(x => x.id);
                    var addableResult = relateRseult.illusts.Where(x => !existingIds.Contains(x.id)).ToList();
                    result.AddRange(addableResult);
                }
            }
        }

        private static void ParallelDownload(IEnumerable<String> urls,int maxDegreeOfParallelism=100)
        {
            Parallel.ForEach(urls, new ParallelOptions() { MaxDegreeOfParallelism = maxDegreeOfParallelism }, async (url) =>
            {
                await Download(url);
            });
        }
        private static async Task Download(string url)
        {
            var client = PixivHttpClientProvier.Value.GetClient(null);

            if (!Directory.Exists(DownloadDir))
                Directory.CreateDirectory(DownloadDir);

            var fileName = ResolveFileName();
            var finalPath = Path.Combine(DownloadDir, fileName);

            if (!File.Exists(finalPath))
            {
                var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, url), CancellationToken.None);
                if (response.Content.Headers.ContentType != null && response.Content.Headers.ContentType.MediaType.Contains("image"))
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
