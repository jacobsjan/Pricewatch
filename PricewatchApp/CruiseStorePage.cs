using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace PricewatchApp
{
    internal class CruiseStorePage : IStorePage
    {
        static Regex priceRegex = new Regex(@"€\s\d+(,\d+)?");
        static Regex cruiseRegex = new Regex(@"\'cruiseId\'\s\:\s\'([A-Z0-9]+)'");
        static Regex itineraryRegex = new Regex(@"\'itineraryId\'\s\:\s\'([A-Z0-9]+)'");
        static Regex nameRegex = new Regex(",\\\"nightName\\\"\\:\\\"(.+?)\\\"");

        public string Price { get; }
        public string ImageUrl { get; }
        public string Name { get; }

        public CruiseStorePage(string url)
        {
            // Download the page from the cruise store
            var web = new HtmlWeb();
            var doc = web.Load(url);

            // Find the cruiseId on the page
            var match = cruiseRegex.Match(doc.DocumentNode.InnerText);
            if (!match.Success) throw (new Exception("CruiseId not found."));
            var cruiseId = match.Groups[1].Value;

            // Download the cruise JSON
            WebClient wc = new WebClient();
            wc.Headers.Add("brand", "hal");
            wc.Headers.Add("currencyCode", "EUR");
            wc.Headers.Add("country", "BE");
            wc.Headers.Add("locale", "nl_NL");
            wc.Headers.Add("Host", "www.hollandamerica.com");
            wc.Headers.Add("Accept", "*/*");

            var json = wc.DownloadString("https://www.hollandamerica.com/api/v2/price/cruise/" + cruiseId + "?");
            var cruise = JToken.Parse(json);

            // Find the price 
            try
            {
                this.Price = cruise.SelectToken("data.roomTypes[?(@.name == 'Verandah')].categories[?(@.name == 'Verandah')].price[0].price").Value<string>() + " €";
            }
            catch (Exception)
            {
                this.Price = "N/A";
            }

            // Find the itineraryID on the page
            match = itineraryRegex.Match(doc.DocumentNode.InnerText);
            if (!match.Success) throw (new Exception("itineraryId not found."));
            var itineraryId = match.Groups[1].Value;

            // Find the cruise route image on the page
            this.ImageUrl = @"https://www.hollandamerica.com/map/itineraries/" + itineraryId + @"/images/en_US_" + itineraryId + @"_mobile_1x.jpg";

            // Find the cruise date
            var date = cruise.SelectToken("data.sailingDate").Value<string>();
            var parsedDate = DateTime.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            
            // Find the cruise name on the page
            match = nameRegex.Match(doc.DocumentNode.InnerText);
            if (!match.Success) throw (new Exception("Cruise name not found."));
            this.Name = match.Groups[1].Value + " (" + parsedDate.ToString("dd MMMM", new CultureInfo("nl-be", false)) + ")";
        }
    }
}
