using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;

namespace PricewatchApp
{
    public static class GetPriceHistory
    {
        [FunctionName("GetPriceHistory")]
        public static HttpResponseMessage Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "GetPriceHistory/appName/{appName}")]HttpRequestMessage req, string appName, TraceWriter log)
        {
            log.Info($"GetPriceHistory function processing request for { appName }.");

            // Retrieve the price history from the database
            using (var db = new PricewatchModel(System.Environment.GetEnvironmentVariable("CONNECTION_STRING")))
            {
                var app = db.Apps.Where(a => a.Name == appName).FirstOrDefault();
                if (app == null) throw new Exception("Invalid appName.");
                var prices = app.Prices.OrderByDescending(a => a.Date).Select(p => new { p.Date, Price = p.Price1 });

                // Return the result
                return req.CreateResponse(HttpStatusCode.OK, prices);
            }
        }
    }
}
