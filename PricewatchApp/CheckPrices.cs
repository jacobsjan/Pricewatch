using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using HtmlAgilityPack;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace PricewatchApp
{
    public static class CheckPrices
    {
        [FunctionName("CheckPrices")]
        public static async System.Threading.Tasks.Task RunAsync([TimerTrigger("0 0 0/12 * * *")]TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"Pricewatch function fired at: {DateTime.Now}");
            Thread.CurrentThread.CurrentCulture = new CultureInfo("nl-BE"); // Otherwise the prices won't parse correctly

            // Start of with an empty mail
            string mailContents = "";
            int imageCounter = 0;
            List<Attachment> attachements = new List<Attachment>();

            // Load apps from database
            using (var db = new PricewatchModel(System.Environment.GetEnvironmentVariable("CONNECTION_STRING")))
            {
                foreach (App app in db.Apps.ToList()) {
                    // Download the page from the app store
                    var url = app.URL;
                    var web = new HtmlWeb();
                    var doc = web.Load(url);

                    // Find the price on the page
                    var price = doc.QuerySelector(".price").InnerText;

                    // Lookup the latestprice in the database
                    var latestPrice = app.Prices.OrderBy(p => p.Date).LastOrDefault();
                    if (latestPrice == null || latestPrice.Price1.CompareTo(price) != 0)
                    {
                        // Add the new price to the database
                        Price newPrice = new Price();
                        newPrice.App1 = app;
                        newPrice.Price1 = price;
                        newPrice.Date = DateTime.Now;
                        app.Prices.Add(newPrice);

                        // Download the app image
                        string imgUrl = doc.QuerySelector("div.product img.artwork").Attributes["src-swap"].Value;
                        using (WebClient client = new WebClient())
                        {
                            byte[] imageData = client.DownloadData(imgUrl);
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
                            float newPriceFloat = float.Parse(price.Substring(0, price.Length - 2));
                            float latestPriceFloat = float.Parse(latestPrice.Price1.Substring(0, latestPrice.Price1.Length - 2));
                            string color = newPriceFloat < latestPriceFloat ? "darkgreen" : "darkred";
                            mailContents += $"<tr><td><p><a href='{app.URL}'><img src='cid:{attachements.Last().ContentId}' width='175' height='175'></td><td style='vertical-align: top;'></a> <a href='{app.URL}'>{app.Name}</a> kost nu <span style='color:{color}'>{price}</span> (in plaats van {latestPrice.Price1})!</p></td></tr>";
                        } else {
                            mailContents += $"<tr><td><p><a href='{app.URL}'><img src='cid:{attachements.Last().ContentId}' width='175' height='175'></td><td style='vertical-align: top;'></a> <a href='{app.URL}'>{app.Name}</a> kost nu {price}!</p></td></tr>";
                        }
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
