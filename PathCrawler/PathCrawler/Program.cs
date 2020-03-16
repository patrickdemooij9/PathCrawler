using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace PathCrawler
{
    class Program
    {
        private static Uri UriTo;

        static async Task Main(string[] args)
        {
            Console.WriteLine(args[0]);
            var uriFrom = new Uri(args[0]);
            var uriTo = new Uri(args[1]);

            UriTo = uriTo;

            Console.WriteLine("Starting crawling from " + uriFrom.ToString() + " to " + uriTo.ToString());
            var webCrawler = new WebCrawler(uriFrom, new HttpClient());

            webCrawler.OnNewLinkAdded += WebCrawler_OnNewLinkAdded;

            await webCrawler.Crawl();

            Console.WriteLine("Could not find url!");
            Console.ReadKey();
        }

        private static void WebCrawler_OnNewLinkAdded(PageModel obj)
        {
            if (UriTo.ToString().Equals(obj.Url.ToString(), StringComparison.InvariantCultureIgnoreCase))
            {
                Console.WriteLine("Url found!");
                var page = obj;
                while(page.Parent != null)
                {
                    Console.WriteLine("URL: " + page.Parent.Url);
                    page = page.Parent;
                }
                Console.ReadKey();
            }
        }
    }
}
