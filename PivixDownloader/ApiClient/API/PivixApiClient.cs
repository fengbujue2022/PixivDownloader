using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EasyHttpClient;
using EasyHttpClient.Attributes;

namespace PivixDownloader.ApiClient.Api
{ 
    public interface IPivixApiClient
    {
        [HttpGet]
        [Authorize]
        [Route("v1/search/illust?filter=for_ios")]
        Task<IHttpResult<string>> SearchIllust(string word, string sort = "date_desc", string search_target = "partial_match_for_tags");
    }
}
