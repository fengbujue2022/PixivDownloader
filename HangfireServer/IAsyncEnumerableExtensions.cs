using Dasync.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HangfireServer
{
    public static class IAsyncEnumerableExtensions
    {
        public static async Task<List<T>> ParallelToListAsync<T>(this Dasync.Collections.IAsyncEnumerable<T> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            var resultList = new List<T>();
            await source.ParallelForEachAsync(async (item) => {
                resultList.Add(item);
            });
            return resultList;
        }
    }
}
