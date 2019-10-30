using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dasync.Collections;
using Hangfire;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using PixivApi.Net.API;
using PixivApi.Net.Model.Response;


// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace HangfireServer
{
    [Route("jobs")]
    public class JobsController : ControllerBase
    {
        private static readonly List<string> EnqueuedFileNames = new List<string>();

        private readonly IPixivApiClient _pixivApiClient;
        private readonly IConfiguration _configuration;
        private readonly IHostingEnvironment _hostingEnvironment;

        public JobsController(IPixivApiClient pixivApiClient, IConfiguration configuration, IHostingEnvironment hostingEnvironment)
        {
            _pixivApiClient = pixivApiClient;
            _configuration = configuration;
            _hostingEnvironment = hostingEnvironment;
        }

        [HttpGet, Route("run")]
        public async Task<string> Run(string keyword = "Arknights")
        {
            //create directory
            GetDownloadFolderPath();

            await Search(
                keyword: keyword,
                bookmarkLimit: 1000,
                takeCount: 10)
            .ParallelForEachAsync(async (illusts) =>
            {
                if (illusts != null && illusts.Any())
                {
                    await UnWrapRecursGetRelated(illusts, bookmarkLimit: 1000, deep: 2).ParallelForEachAsync(async resultList =>
                    {
                        foreach (var r in resultList)
                        {
                            string imageUrl;
                            if (!string.IsNullOrWhiteSpace(r.meta_single_page.original_image_url))
                            {
                                imageUrl = r.meta_single_page.original_image_url;
                            }
                            else
                            {
                                imageUrl = r.image_urls.large;
                            }
                            BackgroundJob.Enqueue(() => Download(imageUrl, $"{r.id}"));
                        }
                    });
                }
            });
            return "All download jobs have enqueued, access https://localhost:5001/hangfire to view more info";
        }

        private Dasync.Collections.IAsyncEnumerable<IEnumerable<Illusts>> Search(string keyword, int bookmarkLimit, int takeCount, int maxCalledCount = 30)
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

        private Dasync.Collections.IAsyncEnumerable<IEnumerable<Illusts>> GetRelated(IEnumerable<Illusts> illusts, int bookmarkLimit)
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

        private Dasync.Collections.IAsyncEnumerable<IEnumerable<Illusts>> UnWrapRecursGetRelated(IEnumerable<Illusts> illusts, int bookmarkLimit, int deep)
        {
            return RecursGetRelated(illusts, bookmarkLimit, deep).Result;
        }

        private async Task<Dasync.Collections.IAsyncEnumerable<IEnumerable<Illusts>>> RecursGetRelated(IEnumerable<Illusts> illusts, int bookmarkLimit, int deep)
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
                await result.ParallelForEachAsync(async (r) =>
                {
                    await yield.ReturnAsync(r);
                });
            });
        }

        [NonAction]
        public async Task Download(string url, string fileName = null)
        {
            if (EnqueueFileName(fileName) == false)
                return;

            var path = GetDownloadFolderPath();

            fileName = fileName == null ? url.Split('/').Last() : $"{fileName}.{Path.GetExtension(url.Split('/').Last())}";
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

        private string GetDownloadFolderPath()
        {
            var folder = _configuration.GetValue<string>("DowloadFolder");
            string path;
            if (Path.IsPathFullyQualified(folder))
                path = folder;
            else
                path = Path.Combine(_hostingEnvironment.ContentRootPath, folder);

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }

        private bool EnqueueFileName(string name)
        {
            var exists = true;
            if (EnqueuedFileNames.IndexOf(name) < 0)
            {
                lock (EnqueuedFileNames)
                {
                    if (EnqueuedFileNames.IndexOf(name) < 0)
                    {
                        exists = false;
                        EnqueuedFileNames.Add(name);
                    }
                }
            }
            return !exists;
        }
    }
}
