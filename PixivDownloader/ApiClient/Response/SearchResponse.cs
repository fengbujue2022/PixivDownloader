using System;
using System.Collections.Generic;
using System.Text;

namespace PixivDownloader.ApiClient.Response
{
    public class IllustsListingResponse
    {
        public IEnumerable<Illusts> illusts { get; set; }
    }

    public class Image_urls
    {
        /// <summary>
        /// 
        /// </summary>
        public string square_medium { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string medium { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string large { get; set; }
    }

    public class Profile_image_urls
    {
        /// <summary>
        /// 
        /// </summary>
        public string medium { get; set; }
    }

    public class User
    {
        /// <summary>
        /// 
        /// </summary>
        public int id { get; set; }
        /// <summary>
        /// 红颜
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string account { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Profile_image_urls profile_image_urls { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string is_followed { get; set; }
    }

    public class TagsItem
    {
        /// <summary>
        /// 珂朵莉
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string translated_name { get; set; }
    }

    public class Meta_single_page
    {
        /// <summary>
        /// 
        /// </summary>
        public string original_image_url { get; set; }
    }

    public class Illusts
    {
        /// <summary>
        /// 
        /// </summary>
        public int id { get; set; }
        /// <summary>
        /// 珂朵莉
        /// </summary>
        public string title { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string type { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Image_urls image_urls { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string caption { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int restrict { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public User user { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<TagsItem> tags { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<string> tools { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string create_date { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int page_count { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int width { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int height { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int sanity_level { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int x_restrict { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string series { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Meta_single_page meta_single_page { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <summary>
        /// 
        /// </summary>
        public int total_view { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int total_bookmarks { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string is_bookmarked { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string visible { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string is_muted { get; set; }
    }

}
