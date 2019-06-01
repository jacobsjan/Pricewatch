using System.Threading.Tasks;

namespace PricewatchApp
{
    public static class StorePageFactory
    {
        public async static Task<IStorePage> Create(string url)
        {
            IStorePage page;
            if (url.StartsWith("https://www.hollandamerica.com"))
            {
                page = new CruiseStorePage();
            } else {
                page = new AppStorePage();
            }
            await page.Load(url);
            return page;
        }
    }
}