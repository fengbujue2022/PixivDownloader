using Dasync.Collections;
using PixivApi.Net.API;
using PixivApi.Net.Model.Response;
using RateLimiter;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Hangfire;
using ConcurrentCollections;

namespace Downloader.Core.Services
{
    public class PixivService : IService
    {
        private static readonly ConcurrentHashSet<string> EnqueuedFileNames = new ConcurrentHashSet<string>();

        private readonly IPixivApiClient _pixivApiClient;
        private readonly IConfiguration _configuration;
        private readonly IHostingEnvironment _hostingEnvironment;

        public PixivService(IPixivApiClient pixivApiClient, IConfiguration configuration, IHostingEnvironment hostingEnvironment)
        {
            _pixivApiClient = pixivApiClient;
            _configuration = configuration;
            _hostingEnvironment = hostingEnvironment;
        }

        public Dasync.Collections.IAsyncEnumerable<IEnumerable<Illusts>> BatchSearch(string keyword, Func<Illusts, bool> predicate, int rows)
        {
            return new AsyncEnumerable<IEnumerable<Illusts>>(async yield =>
            {
                var stack = new ConcurrentStack<int>(Enumerable.Range(0, rows));
                while (stack.TryPop(out var index))
                {
                    var searchResult = await _pixivApiClient.SearchIllust(keyword, offset: index * 30);
                    Trace.WriteLine("The search calling on");
                    searchResult.illusts = searchResult.illusts.Where(predicate).ToList();
                    if (searchResult.illusts.Any())
                    {
                        await yield.ReturnAsync(searchResult.illusts);
                    }
                }
            });
        }

        public Dasync.Collections.IAsyncEnumerable<IEnumerable<Illusts>> GetRelated(IEnumerable<Illusts> illusts, Func<Illusts, bool> predicate)
        {
            return new AsyncEnumerable<IEnumerable<Illusts>>(async yield =>
            {
                foreach (var illustd in illusts)
                {
                    var relateRseult = await _pixivApiClient.IllustRelated(illustd.id);
                    Trace.WriteLine("The releated calling on");
                    relateRseult.illusts = relateRseult.illusts.Where(predicate).ToList();
                    if (relateRseult.illusts.Any())
                    {
                        await yield.ReturnAsync(relateRseult?.illusts);
                    }
                }
            });
        }

        public Dasync.Collections.IAsyncEnumerable<IEnumerable<Illusts>> GetRecursionRelated(IEnumerable<Illusts> illusts, Func<Illusts, bool> predicate, int deep)
        {
            return new AsyncEnumerable<IEnumerable<Illusts>>(async yield =>
            {
                var result = GetRelated(illusts, predicate);
                while (--deep > 0)
                {
                    illusts = (await result.ToListAsync()).SelectMany(x => x);
                    illusts = illusts.GroupBy(x => x.id).Select(x => x.First());
                    result = GetRecursionRelated(illusts, predicate, deep);
                }
                await result.ForEachAsync(async (r) =>
                {
                    await yield.ReturnAsync(r);
                });
            });
        }

        public void EnqueueToDownload(string keyword, Illusts illusts)
        {
            var filename = $"{illusts.id}.{Path.GetExtension(illusts.image_urls.large)}";

            if (_configuration.GetValue<bool>("EnableRatioFilter")
                &&
                (FilterRule.RatioH(illusts) || FilterRule.RatioH(illusts)) == false)
                return;

            if (EnqueuedFileNames.Add(filename) == false)
                return;

            var imageUrl = !string.IsNullOrWhiteSpace(illusts.meta_single_page.original_image_url) ? illusts.meta_single_page.original_image_url : illusts.image_urls.large;
            BackgroundJob.Enqueue(() => Download(keyword, imageUrl, filename));
        }

        public Task Download(string keyword, string url)
        {
            return Download(url, url.Split('/').Last());
        }

        public async Task Download(string keyword, string url, string fileName)
        {
            var path = GetDownloadFolder(keyword);
            var finalPath = Path.Combine(path, fileName);

            if (System.IO.File.Exists(finalPath) == true)
                return;

            var response = await _pixivApiClient.Download(url);
            if (response.ContentHeaders.ContentType != null && response.ContentHeaders.ContentType.MediaType.Contains("image"))
            {
                using (var fileStream = new FileStream(finalPath, FileMode.OpenOrCreate))
                {
                    await response.Content.CopyToAsync(fileStream);
                }
            }
        }

        private string GetDownloadFolder(string keyword)
        {
            var folder = _configuration.GetValue<string>("DowloadFolder");
            string path;
            if (Path.IsPathFullyQualified(folder))
                path = Path.Combine(folder, keyword);
            else
                path = Path.Combine(_hostingEnvironment.ContentRootPath, folder, keyword);

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }
    }
}
