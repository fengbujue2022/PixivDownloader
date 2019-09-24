using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EasyHttpClient;
using EasyHttpClient.Attributes;
using PixivDownloader.ApiClient.Response;

namespace PixivDownloader.ApiClient.Api
{
    //refer to https://github.com/upbit/pixivpy/wiki

    public interface IPixivApiClient
    {
        [HttpGet]
        [Authorize]
        [Route("v1/search/illust")]
        Task<IllustsListingResponse> SearchIllust(string word, string sort = "date_desc", string search_target = "partial_match_for_tags", string filter = "for_ios", int? offset = null);

        [HttpGet]
        [Authorize]
        [Route("v2/illust/related")]
        Task<IllustsListingResponse> IllustRelated(int illust_id, string filter = "for_ios");

        [HttpGet]
        [Authorize]
        [Route("v2/illust/follow")]
        Task<string> IllusFollow(string restrict = "public", int? offset = null);

        [HttpGet]
        [Authorize]
        [Route("v1/illust/detail")]
        Task<Illusts> IllustDetail(string illust_id);

        [HttpGet]
        [Authorize]
        [Route("v1/illust/recommended")]
        Task<string> IllustRecommended(string content_type = "illust", bool include_ranking_label = true, string filter = "for_ios");

        [HttpGet]
        [Authorize]
        [Route("v1/illust/ranking")]
        Task<string> IllustRanking(string mode = "illust", string filter = "for_ios", string date = null, string offset = null);

        [HttpGet]
        [Authorize]
        [Route("v1/user/following")]
        Task<string> UserFollowing(string user_id = "illust", string restrict = "public", string offset = null);
    }
}
