using System.Linq;
using System.Threading.Tasks;
using Dasync.Collections;
using Downloader.Core;
using Downloader.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace HangfireServer
{
    public class JobsController : ControllerBase
    {
        private readonly PixivService _pixivService;

        public JobsController(PixivService pixivService)
        {
            _pixivService = pixivService;
        }

        [HttpGet, Route("run")]
        public async Task<string> Run(string keyword = "Arknights")
        {
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
                           _pixivService.EnqueueToDownload(illust);
                       }
                   });
            });
            return "All download jobs have enqueued, access https://localhost:5001/hangfire to view more info";
        }
    }
}
