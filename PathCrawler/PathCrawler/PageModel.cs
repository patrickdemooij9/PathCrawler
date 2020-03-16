using System;

namespace PathCrawler
{
    public class PageModel
    {
        public Uri Url { get; set; }
        public PageModel Parent { get; set; }

        public PageModel(Uri url, PageModel parent = null)
        {
            Url = url;
            Parent = parent;
        }
    }
}