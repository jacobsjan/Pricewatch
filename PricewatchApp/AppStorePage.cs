using HtmlAgilityPack;
using System;
using System.Net;
using System.Text.RegularExpressions;

namespace PricewatchApp
{
    class AppStorePage
    {
        static Regex priceRegex = new Regex(@"\d+(,\d+)?\s€");

        public string Price { get; }
        public string ImageUrl { get; }
        public string Name { get; }

        public AppStorePage(string url)
        {
            // Download the page from the app store
            var web = new HtmlWeb();
            var doc = web.Load(url);

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
