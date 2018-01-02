using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;

namespace PricewatchApp
{
    public static class DeletePricewatch
    {
        [FunctionName("DeletePricewatch")]
        public static HttpResponseMessage Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "DeletePricewatch/appName/{appName}")]HttpRequestMessage req, string appName, TraceWriter log)
        {
            log.Info($"DeletePricewatch function processing request to delete { appName }.");

            // Add the app tothe database
            using (var db = new PricewatchModel(System.Environment.GetEnvironmentVariable("CONNECTION_STRING")))
            {
                db.Database.ExecuteSqlCommand("DELETE FROM Prices WHERE app=@appName", new SqlParameter("@appName", appName));
                db.Database.ExecuteSqlCommand("DELETE FROM Apps WHERE name=@appName", new SqlParameter("@appName", appName));
            }

            return req.CreateResponse(HttpStatusCode.OK);
        }
    }
}
