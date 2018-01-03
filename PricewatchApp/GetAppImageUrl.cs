using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using HtmlAgilityPack;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;

namespace PricewatchApp
{
    public static class GetAppImageUrl
    {
        [FunctionName("GetAppImageUrl")]
        public static HttpResponseMessage Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "GetAppImageUrl/appName/{appName}")]HttpRequestMessage req, string appName, TraceWriter log)
        {
            log.Info($"GetAppImageUrl function processing request for { appName }.");

            // Retrieve the app URL from the database
            using (var db = new PricewatchModel(System.Environment.GetEnvironmentVariable("CONNECTION_STRING")))
            {
                var url = db.Apps.Where(a => a.Name == appName).FirstOrDefault().URL;
                if (String.IsNullOrEmpty(url)) throw new Exception("Invalid appName.");

                // Fetch the image URL from the AppStore
                var web = new HtmlWeb();
                var doc = web.Load(url);
                string imgUrl = doc.QuerySelector("div.product img.artwork").Attributes["src-swap"].Value;

                // Return the result
                var response = req.CreateResponse(HttpStatusCode.OK, imgUrl);
                response.Headers.CacheControl = new CacheControlHeaderValue() { MaxAge = new TimeSpan(1, 0, 0, 0) }; // One day
                return response;
            }
        }
    }
}
