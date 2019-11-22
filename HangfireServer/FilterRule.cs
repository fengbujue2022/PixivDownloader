using PixivApi.Net.Model.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HangfireServer
{
    public class FilterRule
    {
        public static bool RatioH(Illusts illusts)=> Math.Abs(illusts.height / illusts.width - 9 / 16) < 1 && illusts.width > 1920;
        public static bool RatioV(Illusts illusts) => Math.Abs(illusts.height / illusts.width - 16 / 9) < 1 && illusts.height > 1920;
        public static bool Bookmark1(Illusts illusts) => illusts.total_bookmarks >= 1000;
        public static bool Bookmark2(Illusts illusts) => illusts.total_bookmarks >= 2000;
        public static bool IllustType(Illusts illusts) => "Illust".Equals(illusts.type,StringComparison.OrdinalIgnoreCase);
    }
}
