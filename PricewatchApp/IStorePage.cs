using System.Threading.Tasks;

namespace PricewatchApp
{
    public interface IStorePage
    {
        string Price { get; }
        string ImageUrl { get; }
        string Name { get; }
        Task Load(string url);
    }
}