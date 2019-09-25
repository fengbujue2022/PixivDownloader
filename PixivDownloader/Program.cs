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
using Newtonsoft.Json;

namespace PixivDownloader
{
    class Program
    {
        //configurable for yourself
        private static readonly string Username = "";
        private static readonly string Password = "";
        private static readonly string DownloadDir = @"C:\Pixiv3";

        private static Lazy<PixivHttpClientProvier> PixivHttpClientProvier = new Lazy<PixivHttpClientProvier>();
        private static PixivApiClientFactory factory = new PixivApiClientFactory(Username, Password);
        private static IPixivApiClient pixivApiClient = factory.Create<IPixivApiClient>();

        private static object _syncObject = new object();

        static async Task Main(string[] args)
        {
            var hbSearchResult = await SearchThenTakeHighlyBookmark(
                keyword: "間桐桜",
                bookmarkLimit: 1000,
                takeCount: 10);

            //深度大于3以上会请求太多api  建议值为3以下
            var relateResult = await RecursGetRelatedWithSearchResult(
                illusts: hbSearchResult,
                bookmarkLimit: 1000,
                deep: 2);

            ParallelDownload(relateResult.Select(x => x.image_urls.square_medium).Where(x => !string.IsNullOrWhiteSpace(x)));
        }

        private static async Task<IEnumerable<Illusts>> SearchThenTakeHighlyBookmark(string keyword, int bookmarkLimit, int takeCount, int maxCalledCount = 30, int parallelism = 2)
        {
            var cOffset = 1;

            var illusts = new List<Illusts>();

            while (maxCalledCount > 0)
            {
                await Task.WhenAll(Enumerable.Range(cOffset, parallelism).Select(x => DoSearch(x)));
                if (illusts.Count >= takeCount)
                {
                    break;
                }
                maxCalledCount--;
                cOffset += parallelism;
            }
            return illusts.Take(takeCount);

            async Task DoSearch(int offset)
            {
                var searchResult = await pixivApiClient.SearchIllust(keyword, offset: offset);
                if (searchResult != null)
                {
                    illusts.AddRange(searchResult.illusts.Where(x => x.total_bookmarks > bookmarkLimit).ToList());
                }
            }
        }

        private static async Task<IEnumerable<Illusts>> GetRelatedWithSearchResult(IEnumerable<Illusts> illusts, int bookmarkLimit)
        {
            var result = new List<Illusts>();
            result.AddRange(illusts);
            await Task.WhenAll(illusts.Select(x => DoGetRelated(x.id)).ToList());
            return result;

            async Task DoGetRelated(int illustId)
            {
                var relateRseult = await pixivApiClient.IllustRelated(illustId);
                if (relateRseult != null)
                {
                    var existingIds = result.Select(x => x.id);
                    var addableResult = relateRseult.illusts.Where(x => x.total_bookmarks > bookmarkLimit && !existingIds.Contains(x.id)).ToList();
                    result.AddRange(addableResult);
                }
            }
        }

        private static async Task<IEnumerable<Illusts>> RecursGetRelatedWithSearchResult(IEnumerable<Illusts> illusts, int bookmarkLimit, int deep)
        {
            if (deep > 0)
            {
                illusts = await GetRelatedWithSearchResult(illusts, bookmarkLimit);
                deep--;
                illusts = await RecursGetRelatedWithSearchResult(illusts, bookmarkLimit, deep);
            }
            return illusts;
        }

        private static void ParallelDownload(IEnumerable<String> urls, int parallelism = 100)
        {
            Parallel.ForEach(urls, new ParallelOptions() { MaxDegreeOfParallelism = parallelism }, (url) =>
            {
                Download(url).Wait();
            });
        }

        private static async Task Download(string url)
        {
            var client = PixivHttpClientProvier.Value.GetClient(null);

            //1. Check directory
            if (!Directory.Exists(DownloadDir))
                Directory.CreateDirectory(DownloadDir);

            var fileName = ResolveFileName();
            var finalPath = Path.Combine(DownloadDir, fileName);

            //2. Create file
            FileStream fs = null;
            if (!File.Exists(finalPath))
            {
                lock (_syncObject)
                {
                    if (!File.Exists(finalPath))
                    {
                        fs = new FileStream(finalPath, FileMode.CreateNew);
                    }
                }
            }

            //3. Copy stream
            if (fs != null)
            {
                var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, url), CancellationToken.None);
                if (response.Content.Headers.ContentType != null && response.Content.Headers.ContentType.MediaType.Contains("image"))
                {
                    using (fs)
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
