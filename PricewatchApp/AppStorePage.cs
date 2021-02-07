using HtmlAgilityPack;
using System;
using System.Net;
using System.Text;
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
            var data = await wc.DownloadDataTaskAsync(url);
            var html = Encoding.UTF8.GetString(data);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Find the price on the page
            this.Price = doc.QuerySelector(".app-header__list__item--price").InnerText;
            var match = priceRegex.Match(this.Price);
            if (match.Success) this.Price = match.Value;

            // Find the app image on the page
            this.ImageUrl = doc.QuerySelector(".we-artwork__source").Attributes["srcset"].Value.Split(' ')[0];

            // Find the app name on the page
            this.Name = WebUtility.HtmlDecode(doc.QuerySelector(".app-header__title").ChildNodes[0].InnerText).Trim();
        }
    }
}
