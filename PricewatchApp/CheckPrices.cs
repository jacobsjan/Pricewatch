using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace PricewatchApp
{
    public static class CheckPrices
    {
        [FunctionName("CheckPrices")]
        public static async System.Threading.Tasks.Task RunAsync([TimerTrigger("0 0 0 * * *")]TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"CheckPrices function fired at: {DateTime.Now}");
            
            // Start of with an empty mail
            string mailContents = "";
            int imageCounter = 0;
            List<Attachment> attachements = new List<Attachment>();

            // Load apps from database
            using (var db = new PricewatchModel(System.Environment.GetEnvironmentVariable("CONNECTION_STRING")))
            {
                foreach (App app in db.Apps.ToList()) {
                    try {
                        // Download and parse AppStore page
                        var page = await StorePageFactory.Create(app.URL);

                        // Lookup the latestprice in the database
                        var latestPrice = app.Prices.OrderBy(p => p.Date).LastOrDefault();
                        if (latestPrice == null || latestPrice.Price1.CompareTo(page.Price) != 0)
                        {
                            // Add the new price to the database
                            Price newPrice = new Price
                            {
                                App1 = app,
                                Price1 = page.Price,
                                Date = DateTime.Now
                            };
                            app.Prices.Add(newPrice);

                            // Download the app image
                            using (WebClient client = new WebClient())
                            {
                                byte[] imageData = client.DownloadData(page.ImageUrl);
                                string imgBase64 = System.Convert.ToBase64String(imageData);

                                attachements.Add(new Attachment
                                {
                                    Content = imgBase64,
                                    ContentId = Guid.NewGuid().ToString(),
                                    Filename = "image" + imageCounter++ + ".jpg", 
                                    Type = "image/jpeg",
                                    Disposition = "inline"
                                });
                            }

                            // Add info to the mail contents
                            if (latestPrice != null)
                            {
                                float.TryParse(page.Price.Substring(0, page.Price.Length - 2).Replace(',', '.'), out float newPriceFloat);
                                float.TryParse(latestPrice.Price1.Substring(0, latestPrice.Price1.Length - 2).Replace(',', '.'), out float latestPriceFloat);

                                string color = newPriceFloat < latestPriceFloat ? "darkgreen" : "darkred";
                                mailContents += $"<tr><td><p><a href='{app.URL}'><img src='cid:{attachements.Last().ContentId}' width='175' height='175'></td><td style='vertical-align: top;'></a> <a href='{app.URL}'>{app.Name}</a> kost nu <span style='color:{color}'>{page.Price}</span> (in plaats van {latestPrice.Price1})!</p></td></tr>";
                            } else {
                                mailContents += $"<tr><td><p><a href='{app.URL}'><img src='cid:{attachements.Last().ContentId}' width='175' height='175'></td><td style='vertical-align: top;'></a> <a href='{app.URL}'>{app.Name}</a> kost nu {page.Price}!</p></td></tr>";
                            }
                        }
                    } catch (Exception e) { 
                        // Ignore it if something went wrong, check the next price
                        log.Error(e.Message, e);
                    }
                }

                // Were any new prices detected?
                if (mailContents.Length > 0)
                {
                    // Send an e-mail
                    var msg = new SendGridMessage();
                    var mailaddress = System.Environment.GetEnvironmentVariable("MAIL_ADDRESS");
                    var mailName = System.Environment.GetEnvironmentVariable("MAIL_NAME");
                    msg.SetFrom(new EmailAddress(mailaddress, mailName));
                    msg.AddTo(new EmailAddress(mailaddress, mailName));
                    msg.SetSubject("iOS AppStore PriceWatch update");
                    msg.AddAttachments(attachements);
                    msg.AddContent(MimeType.Html, "<table>" + mailContents + "</table>");

                    var apiKey = System.Environment.GetEnvironmentVariable("SENDGRID_APIKEY");
                    var client = new SendGridClient(apiKey);
                    var response = await client.SendEmailAsync(msg);

                    // Commit new prices to the database
                    db.SaveChanges();
                }
            }
        }
    }
}
