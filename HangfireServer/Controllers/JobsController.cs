using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ConcurrentCollections;
using Dasync.Collections;
using Hangfire;
using HangfireServer.Extensions;
using HangfireServer.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using PixivApi.Net.API;
using PixivApi.Net.Model.Response;
using RateLimiter;

namespace HangfireServer
{
    public class JobsController : ControllerBase
    {
        private static readonly ConcurrentHashSet<string> EnqueuedFileNames = new ConcurrentHashSet<string>();

        private readonly IPixivApiClient _pixivApiClient;
        private readonly PixivService _pixivService;
        private readonly IConfiguration _configuration;
        private readonly IHostingEnvironment _hostingEnvironment;

        public JobsController(IPixivApiClient pixivApiClient, PixivService pixivService, IConfiguration configuration, IHostingEnvironment hostingEnvironment)
        {
            _pixivApiClient = pixivApiClient;
            _pixivService = pixivService;
            _configuration = configuration;
            _hostingEnvironment = hostingEnvironment;
        }

        [HttpGet, Route("run")]
        public async Task<string> Run(string keyword = "Arknights")
        {
            GetDownloadFolder();
            await _pixivService.BatchSearch(keyword, FilterRule.Bookmark1, 20).ForEachAsync(async (illusts) =>
            {
                await _pixivService.GetRecursionRelated(
                    illusts,
                    (illust) =>
                    {
                        return FilterRule.Bookmark2(illust) && FilterRule.IllustType(illust);
                    },
                    deep: 2)
                    .ForEachAsync(relatedIllusts =>
                   {
                       foreach (var illust in relatedIllusts)
                       {
                           EnqueueToDownload(illust);
                       }
                   });
            });
            return "All download jobs have enqueued, access https://localhost:5001/hangfire to view more info";
        }

        [NonAction]
        public async Task Download(string url, string fileName = null)
        {
            var path = GetDownloadFolder();

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

        private void EnqueueToDownload(Illusts illusts)
        {
            var filename = $"{illusts.id}";

            if (_configuration.GetValue<bool>("EnableRatioFilter")
                &&
                (FilterRule.RatioH(illusts) || FilterRule.RatioH(illusts)) == false)
                return;

            if (EnqueuedFileNames.Add(filename) == false)
                return;

            var imageUrl = !string.IsNullOrWhiteSpace(illusts.meta_single_page.original_image_url) ? illusts.meta_single_page.original_image_url : illusts.image_urls.large;
            BackgroundJob.Enqueue(() => Download(imageUrl, filename));
        }

        private string GetDownloadFolder()
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
    }
}
