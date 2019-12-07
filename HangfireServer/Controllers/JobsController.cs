using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dasync.Collections;
using Downloader.Core;
using Downloader.Core.Services;
using Microsoft.AspNetCore.Mvc;
using PixivApi.Net.API;
using PixivApi.Net.Model.Response;

namespace HangfireServer
{
    public class JobsController : ControllerBase
    {
        private readonly PixivService _pixivService;
        private readonly IPixivApiClient _pixivApiClient;

        public JobsController(PixivService pixivService, IPixivApiClient pixivApiClient)
        {
            _pixivService = pixivService;
            _pixivApiClient = pixivApiClient;
        }

        [HttpGet, Route("run")]
        public async Task<string> Run(string keyword = "Arknights", bool useAutocomplate = false)
        {
            if (useAutocomplate)
            {
                var suggestionKeyword = await _pixivApiClient.SearchAutocomplete(keyword);
                if (suggestionKeyword.tags != null && suggestionKeyword.tags.Any())
                {
                    keyword =
                        suggestionKeyword.tags.FirstOrDefault(x => !string.IsNullOrEmpty(x.translated_name))?.translated_name
                        ??
                        suggestionKeyword.tags.FirstOrDefault(x => !string.IsNullOrEmpty(x.name))?.name
                        ??
                        keyword;
                }
            }
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
                          _pixivService.EnqueueToDownload(keyword, illust);
                      }
                  });
            });
            return "All download jobs have enqueued, access https://localhost:5001/hangfire to view more info";
        }
    }
}
