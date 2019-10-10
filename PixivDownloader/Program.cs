using Dasync.Collections;
using PixivApi;
using PixivApi.Api;
using PixivApi.Model.Response;
using PixivApi.OAuth;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace PixivDownloader
{
    class Program
    {
        //configurable for yourself
        private static readonly string Username = "";
        private static readonly string Password = "";
        private static readonly string DownloadDir = @"C:\Pixiv3";

        private static readonly PixivApiClientFactory _factory = new PixivApiClientFactory(Username, Password, new TextFileAuthStore(), TimeSpan.FromSeconds(15));
        private static readonly IPixivApiClient _pixivApiClient = _factory.Create<IPixivApiClient>();
        private static readonly TaskQueue _taskQueue = new TaskQueue(20);

        private static object _syncObject = new object();

        static async Task Main(string[] args)
        {
            await Search(
                keyword: "Arknights",
                bookmarkLimit: 1000,
                takeCount: 10)
            .ParallelForEachAsync(async (illusts) =>
            {
                if (illusts != null && illusts.Any())
                {
                    await UnWrapRecursGetRelated(illusts, bookmarkLimit: 1000, deep: 2).ForEachAsync(resultList =>
                    {
                          foreach (var r in resultList)
                          {
                              AddToDownloadQueue(r.image_urls.square_medium);
                          }
                    });
                }
            });

            await _taskQueue.WaitAll();//让主线程等待所有任务完成
        }

        private static IAsyncEnumerable<IEnumerable<Illusts>> Search(string keyword, int bookmarkLimit, int takeCount, int maxCalledCount = 30)
        {
            return new AsyncEnumerable<IEnumerable<Illusts>>(async yield =>
            {
                var offset = 30;
                var pageIndex = 0;
                var count = 0;
                while (maxCalledCount > 0)
                {
                    if (count >= takeCount)
                    {
                        yield.Break();
                    }
                    var r = await DoSearch(pageIndex * offset);
                    await yield.ReturnAsync(r);
                    count += r.Count();
                    pageIndex++;
                    maxCalledCount--;
                }
            });

            async Task<IEnumerable<Illusts>> DoSearch(int offset)
            {
                var searchResult = await _pixivApiClient.SearchIllust(keyword, offset: offset);

                if (searchResult?.illusts != null)
                {
                    searchResult.illusts = searchResult.illusts.Where(x => x.total_bookmarks > bookmarkLimit);
                }
                return searchResult?.illusts;
            }
        }

        private static IAsyncEnumerable<IEnumerable<Illusts>> GetRelated(IEnumerable<Illusts> illusts, int bookmarkLimit)
        {
            return new AsyncEnumerable<IEnumerable<Illusts>>(async yield =>
            {
                foreach (var illustd in illusts)
                {
                    var r = await DoGetRelated(illustd.id);
                    await yield.ReturnAsync(r);
                }
            });
            async Task<IEnumerable<Illusts>> DoGetRelated(int illustId)
            {
                var relateRseult = await _pixivApiClient.IllustRelated(illustId);
                if (relateRseult?.illusts != null)
                {
                    relateRseult.illusts = relateRseult.illusts.Where(x =>
                        x.total_bookmarks > bookmarkLimit
                        && x.type == "illust").ToList();
                }
                return relateRseult?.illusts;
            }
        }


        private static IAsyncEnumerable<IEnumerable<Illusts>> UnWrapRecursGetRelated(IEnumerable<Illusts> illusts, int bookmarkLimit, int deep)
        {
            return RecursGetRelated(illusts, bookmarkLimit, deep).Result;
        }

        private static async Task<IAsyncEnumerable<IEnumerable<Illusts>>> RecursGetRelated(IEnumerable<Illusts> illusts, int bookmarkLimit, int deep)
        {
            var result = GetRelated(illusts, bookmarkLimit);
            if (deep > 0)
            {
                illusts = (await result.ParallelToListAsync()).SelectMany(x => x);
                illusts = illusts.GroupBy(x => x.id).Select(x => x.First());//distinct
                result = await RecursGetRelated(illusts, bookmarkLimit, --deep);
            }
            return new AsyncEnumerable<IEnumerable<Illusts>>(async yield =>
            {
                await result.ForEachAsync(async (r) =>
                {
                    await yield.ReturnAsync(r);
                });
            });
        }

        private static void AddToDownloadQueue(string url)
        {
            _taskQueue.Queue(async () => await Download(url));
        }

        private static async Task Download(string url)
        {
            //1. Check directory
            if (!Directory.Exists(DownloadDir))
                Directory.CreateDirectory(DownloadDir);

            //2. Create file
            FileStream fs = null;
            var finalPath = Path.Combine(DownloadDir, ResolveFileName());
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
                var response = await _pixivApiClient.Download(url);
                if (response.ContentHeaders.ContentType != null && response.ContentHeaders.ContentType.MediaType.Contains("image"))
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
