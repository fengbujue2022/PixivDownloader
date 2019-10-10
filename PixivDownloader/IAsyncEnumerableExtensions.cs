using Dasync.Collections;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PixivDownloader
{
    public static class IAsyncEnumerableExtensions
    {
        public static async Task<List<T>> ParallelToListAsync<T>(this IAsyncEnumerable<T> source, CancellationToken cancellationToken = default)
        {
            var resultList = new List<T>();
            await source.ParallelForEachAsync(async (item) => {
                resultList.Add(item);
            });
            return resultList;
        }
    }
}
