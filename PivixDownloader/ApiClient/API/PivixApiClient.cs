using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EasyHttpClient;
using EasyHttpClient.Attributes;
using PivixDownloader.ApiClient.Response;

namespace PivixDownloader.ApiClient.Api
{
    public interface IPivixApiClient
    {
        [HttpGet]
        [Authorize]
        [Route("v1/search/illust")]
        Task< IllustsListingResponse> SearchIllust(string word, string sort = "date_desc", string search_target = "partial_match_for_tags", string filter = "for_ios");

        [HttpGet]
        [Authorize]
        [Route("v2/illust/related")]
        Task<IllustsListingResponse> IllustRelated(int illust_id, string filter = "for_ios");
    }
}
