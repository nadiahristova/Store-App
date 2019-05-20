using System.Threading.Tasks;

namespace InventoryManager.Service.Interfaces
{
    public interface IInventoryManagerService
    {
        Task ImportBookInvenoryCatalog(string bookInventoryInfo);

        Task<int> ReturnQuantityByBookTitle(string bookTitle, string authorFullName);

        Task<decimal> CalculateBookBasketPrice(params string[] bookTitlesInBasket);
    }
}
