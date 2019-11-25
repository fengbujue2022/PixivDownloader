using EasyHttpClient.ActionFilters;
using PixivApi.Net;
using RateLimiter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ComposableAsync;
using EasyHttpClient;

namespace HangfireServer.Code
{
    public class RateLimitFilterAttribute : ActionFilterAttribute
    {
        public readonly TimeLimiter _timeLimiter;

        public RateLimitFilterAttribute(TimeLimiter timeLimiter)
        {
            _timeLimiter = timeLimiter;
        }

        public override async Task<IHttpResult> ActionInvoke(ActionContext context, Func<Task<IHttpResult>> continuation)
        {
            await _timeLimiter;
            return await continuation();
        }
    }
}
