using HtmlAgilityPack;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace PathCrawler
{
    public class WebCrawler
    {
        public event Action<Uri, HtmlDocument> OnPageLoaded;
        public event Action<PageModel> OnNewLinkAdded;

        private readonly HttpClient _httpClient;
        private readonly ConcurrentQueue<PageModel> _pages;
        private readonly ConcurrentBag<PageModel> _visitedPages;
        private readonly Uri _uri;

        public WebCrawler(Uri uri, HttpClient httpClient)
        {
            _uri = uri;
            _httpClient = httpClient;
            _pages = new ConcurrentQueue<PageModel>();
            _visitedPages = new ConcurrentBag<PageModel>();
        }

        public async Task Crawl()
        {
            PageModel currentNode = new PageModel(_uri);
            _pages.Enqueue(currentNode);

            while (_pages.Any())
            {
                if (_pages.TryDequeue(out PageModel page))
                {
                    _visitedPages.Add(page);
                    var result = await CrawlNode(page);
                    Console.WriteLine($"Finished crawling {page.Url.ToString()} with result: {result.ToString()}. {_pages.Count} pages left to visit!");
                    continue;
                }
                return;
            }
        }

        private async Task<PageCrawlResult> CrawlNode(PageModel node)
        {
            try
            {
                var response = await _httpClient.GetAsync(node.Url);

                Uri requestUrl = response.RequestMessage.RequestUri;
                requestUrl = new Uri(requestUrl.GetLeftPart(UriPartial.Path));

                if (requestUrl != node.Url)
                {
                    if (requestUrl.Host.Replace("www.", "") != node.Url.Host.Replace("www.", ""))
                        return PageCrawlResult.FAILED;
                    else if (ShouldAddUrl(requestUrl))
                    {
                        var pageModel = new PageModel(requestUrl, node);
                        OnNewLinkAdded?.Invoke(pageModel);
                        _pages.Enqueue(pageModel);
                    }
                    return PageCrawlResult.FINISHED;
                }

                if (response.IsSuccessStatusCode)
                {
                    var pageContent = await response.Content.ReadAsStringAsync();
                    var pageDocument = new HtmlDocument();

                    pageDocument.LoadHtml(pageContent);

                    OnPageLoaded?.Invoke(node.Url, pageDocument);

                    foreach (HtmlNode link in pageDocument.DocumentNode.SelectNodes("//a[@href]"))
                    {
                        HtmlAttribute att = link.Attributes["href"];
                        string value = att.Value;
                        if (value.StartsWith("#") || value.StartsWith("mailto:"))
                            continue;

                        string extension = Path.GetExtension(value);
                        if (extension != string.Empty && extension != ".html")
                            continue;

                        Uri linkUri;
                        if (!Uri.TryCreate(requestUrl, value, out linkUri))
                            continue;

                        if (linkUri.Host != requestUrl.Host)
                            continue;

                        linkUri = new Uri(linkUri.GetLeftPart(UriPartial.Path));

                        if (ShouldAddUrl(linkUri))
                        {
                            var pageModel = new PageModel(linkUri, node);
                            OnNewLinkAdded?.Invoke(pageModel);
                            _pages.Enqueue(pageModel);
                        }
                    }
                }
                else if (response.StatusCode.Equals(HttpStatusCode.Moved))
                {
                    string returnURL = response.Headers.GetValues("Location").FirstOrDefault().ToString();
                    Uri linkUri = new Uri(new Uri(returnURL).GetLeftPart(UriPartial.Path));

                    if (linkUri.Host != requestUrl.Host)
                        return PageCrawlResult.FAILED;

                    if (ShouldAddUrl(linkUri))
                    {
                        var pageModel = new PageModel(linkUri, node);
                        OnNewLinkAdded?.Invoke(pageModel);
                        _pages.Enqueue(pageModel);
                    }
                }
                return PageCrawlResult.FINISHED;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return PageCrawlResult.ERROR;
            }
        }

        private bool ShouldAddUrl(Uri uri)
        {
            return !_pages.Any(m => m.Url.Equals(uri)) && !_visitedPages.Any(m => m.Url.Equals(uri));
        }
    }
}
