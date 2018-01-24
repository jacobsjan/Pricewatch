using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;

namespace PricewatchApp
{
    public static class AddPricewatch
    {
        [FunctionName("AddPricewatch")]
        public static HttpResponseMessage Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "AddPricewatch/url/{url}")]HttpRequestMessage req, string url, TraceWriter log)
        {
            url = WebUtility.UrlDecode(url);
            log.Info($"AddPricewatch function processing request for { url }.");
            
            // Fetch the app page from the AppStore
            var web = new HtmlAgilityPack.HtmlWeb();
            var doc = web.Load(url);

            // Construct new app 
            App newApp = new App
            {
                Name = doc.QuerySelector("h1[itemprop=name]").InnerText,
                URL = url
            };
            newApp.Prices.Add(new Price
            {
                Date = DateTime.Now,
                Price1 = doc.QuerySelector(".price").InnerText
            });

            // Add the app tothe database
            using (var db = new PricewatchModel(System.Environment.GetEnvironmentVariable("CONNECTION_STRING")))
            {
                db.Apps.Add(newApp);
                db.SaveChanges();
            }

            // Construct response
            var pricewatch = new {
                newApp.Name,
                newApp.URL,
                Price = newApp.Prices.ElementAt(0).Price1,
                PreviousPrice = "",
                ImageUrl = doc.QuerySelector("div.product-hero__media img.we-artwork__image").Attributes["src"].Value
            };

            // Return results as JSON
            var jsonToReturn = JsonConvert.SerializeObject(pricewatch);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonToReturn, Encoding.UTF8, "application/json")
            };
        }
    }
}
