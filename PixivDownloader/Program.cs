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
        //private static readonly string Username = "";
        //private static readonly string Password = "";
        private static readonly string Username = "";
        private static readonly string Password = "";
        private static readonly string DownloadDir = @"C:\Pixiv3";

        private static Lazy<PixivHttpClientProvier> PixivHttpClientProvier = new Lazy<PixivHttpClientProvier>();
        private static PixivApiClientFactory factory = new PixivApiClientFactory(Username, Password);
        private static IPixivApiClient pixivApiClient = factory.Create<IPixivApiClient>();

        static async Task Main(string[] args)
        {
            var hbSearchResult = await SearchThenTakeHighlyBookmark(
                keyword: "heaven's feel",
                bookmarkLimit: 1000,
                takeCount: 5);

            //深度大于3以上会请求太多api  建议值为3以下
            var relateResult = await RecursGetRelatedWithSearchResult(
                illusts: hbSearchResult,
               bookmarkLimit: 1000,
               deep: 2);

            ParallelDownload(relateResult.Select(x => x.meta_single_page.original_image_url).Where(x => !string.IsNullOrWhiteSpace(x)));
        }

        private static async Task<IEnumerable<Illusts>> SearchThenTakeHighlyBookmark(string keyword, int bookmarkLimit, int takeCount)
        {
            var maxCalledCount = 20;
            var cOffset = 1;
            var prallelism = 2;

            var illusts = new List<Illusts>();

            while (maxCalledCount > 0)
            {
                await Task.WhenAll(Enumerable.Range(cOffset, prallelism).Select(x => DoSearch(x)));
                if (illusts.Count >= takeCount)
                {
                    break;
                }
                maxCalledCount--;
                cOffset += prallelism;
            }
            return illusts.Take(takeCount);

            async Task DoSearch(int offset)
            {
                var searchResult = await pixivApiClient.SearchIllust(keyword, offset: offset);
                Console.WriteLine("called ");
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
                Console.WriteLine("called "+ illustId);
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

        private static void ParallelDownload(IEnumerable<String> urls, int maxDegreeOfParallelism = 100)
        {
            Parallel.ForEach(urls, new ParallelOptions() { MaxDegreeOfParallelism = maxDegreeOfParallelism }, (url) =>
           {
               Download(url).Wait();
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
