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

namespace HangfireServer.Services
{
    public class PixivService : IService
    {
        private readonly IPixivApiClient _pixivApiClient;

        public PixivService(IPixivApiClient pixivApiClient)
        {
            _pixivApiClient = pixivApiClient;
        }

        public Dasync.Collections.IAsyncEnumerable<IEnumerable<Illusts>> BatchSearch(string keyword, Func<Illusts, bool> predicate, int rows)
        {
            return new AsyncEnumerable<IEnumerable<Illusts>>(async yield =>
            {
                var stack = new ConcurrentStack<int>(Enumerable.Range(0, rows));
                while (stack.TryPop(out var index))
                {
                    var searchResult = await _pixivApiClient.SearchIllust(keyword, offset: index * 30);
                    Trace.WriteLine("search on called");
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
                    Trace.WriteLine("relate on called");
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

    }
}
