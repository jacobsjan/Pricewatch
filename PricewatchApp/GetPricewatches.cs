using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;

namespace PricewatchApp
{
    public static class GetPricewatches
    {
        [FunctionName("GetPricewatches")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("GetPricewatches function processing a request.");

            // Retrieve pricewatches from database
            using (var db = new PricewatchModel(System.Environment.GetEnvironmentVariable("CONNECTION_STRING")))
            {
                // Retrieve pricewatch results
                var pricewatches = db.Apps.Select(app => new {
                    app.Name,
                    app.URL,
                    Price = app.Prices.Where(price => price.Date == app.Prices.Max(p => p.Date)).FirstOrDefault().Price1,
                    PreviousPrice = app.Prices.OrderByDescending(p => p.Date).Skip(1).FirstOrDefault().Price1,
                    ImageUrl = "p.png"
                });

                // Return results as JSON
                var jsonToReturn = JsonConvert.SerializeObject(pricewatches);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(jsonToReturn, Encoding.UTF8, "application/json")
                };   
            }
        }
    }
}
