using HtmlAgilityPack;
using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PricewatchApp
{
    class AppStorePage: IStorePage
    {
        static Regex priceRegex = new Regex(@"\d+(,\d+)?\s€");

        public string Price { get; set; }
        public string ImageUrl { get; set; }
        public string Name { get; set; }

        public async Task Load(string url)
        {
            // Download the page from the app store
            var wc = new WebClient();
            var html = await wc.DownloadStringTaskAsync(new Uri(url));
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Find the price on the page
            this.Price = doc.QuerySelector(".inline-list__item--bulleted").InnerText;
            var match = priceRegex.Match(this.Price);
            if (match.Success) this.Price = match.Value;

            // Find the app image on the page
            this.ImageUrl = doc.QuerySelector("div.product-hero__media img.we-artwork__image").Attributes["src"].Value;

            // Find the app name on the page
            this.Name = WebUtility.HtmlDecode(doc.QuerySelector(".product-header__title").ChildNodes[0].InnerText).Trim();
        }
    }
}
