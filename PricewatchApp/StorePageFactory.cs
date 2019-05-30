namespace PricewatchApp
{
    public static class StorePageFactory
    {
		public static IStorePage Create(string url)
        {
			if (url.StartsWith("https://www.hollandamerica.com"))
            {
                return new CruiseStorePage(url);
            } else {
                return new AppStorePage(url);
            }
        }
    }
}