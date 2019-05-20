using InventoryManager.Service;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.EntityFrameworkCore;
using InventoryManager.Data;
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using InventoryManager.Service.Interfaces;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using InventoryManager.Service.Exceptions;

namespace InventoryManager.Test
{
    [TestClass]
    public class BookInventoryTest
    {
        private static InventoryManagerService _inventoryManagerService;

        public BookInventoryTest()
        {
        }

        [TestInitialize]
        public async Task Setup()
        {
            IServiceCollection services = new ServiceCollection();

            services.AddHostedService<InventoryManagerService>();
            services.AddDbContext<InventoryManagerContext>(opts => opts.UseInMemoryDatabase(new Guid().ToString()));
            services.AddSingleton<IInventoryManagerService, InventoryManagerService>();
            var serviceProvider = services.BuildServiceProvider();

            _inventoryManagerService = serviceProvider.GetService<IHostedService>() as InventoryManagerService;

            string text = System.IO.File.ReadAllText(@"../../../Resources/TestImportData.txt");

            await _inventoryManagerService.ImportBookInvenoryCatalog(text);
        }


        [TestMethod]
        public async Task GetNumberOfBooks()
        {
            var quantityGoblet = await _inventoryManagerService.ReturnQuantityByBookTitle("Goblet Of fire", "J.K Rowling");
            var quantityFountainHead = await _inventoryManagerService.ReturnQuantityByBookTitle("FountainHead", "Ayn Rand");
            var quantityFoundation = await _inventoryManagerService.ReturnQuantityByBookTitle("Foundation", "Isaac Asimov");
            var quantityRobot = await _inventoryManagerService.ReturnQuantityByBookTitle("Robot series", "Isaac Asimov");
            var quantityAssassin = await _inventoryManagerService.ReturnQuantityByBookTitle("Assassin Apprentice", "Robin Hobb");

            Assert.AreEqual(quantityGoblet, 2);
            Assert.AreEqual(quantityFountainHead, 10);
            Assert.AreEqual(quantityFoundation, 1);
            Assert.AreEqual(quantityRobot, 1);
            Assert.AreEqual(quantityAssassin, 8);
        }


        [TestMethod]
        public async Task CalculateBasketPrice()
        {
            var priceGoblet = await _inventoryManagerService.CalculateBookBasketPrice("J.K Rowling - Goblet Of fire");
            string[] basket1 = Regex.Split("J.K Rowling - Goblet Of fire, J.K Rowling - Goblet Of fire", Utils.Constants.BookBasketEntriesSplitRegex);
            var priceBasket1 = await _inventoryManagerService.CalculateBookBasketPrice(basket1);
            string[] basket2 = Regex.Split("J.K Rowling - Goblet Of fire, J.K Rowling - Goblet Of fire, Robin Hobb - Assassin Apprentice", Utils.Constants.BookBasketEntriesSplitRegex);
            var priceBasket2 = await _inventoryManagerService.CalculateBookBasketPrice(basket2);
            string[] basket3 = Regex.Split("J.K Rowling - Goblet Of fire, Robin Hobb - Assassin Apprentice, Robin Hobb - Assassin Apprentice", Utils.Constants.BookBasketEntriesSplitRegex);
            var priceBasket3 = await _inventoryManagerService.CalculateBookBasketPrice(basket3);
            string[] basket4 = Regex.Split("Ayn Rand - FountainHead, Isaac Asimov - Foundation, Isaac Asimov - Robot series, J.K Rowling - Goblet Of fire, J.K Rowling - Goblet Of fire, Robin Hobb - Assassin Apprentice, Robin Hobb - Assassin Apprentice", Utils.Constants.BookBasketEntriesSplitRegex);
            var priceBasket4 = await _inventoryManagerService.CalculateBookBasketPrice(basket4);

            Assert.AreEqual(priceGoblet, 8);
            Assert.AreEqual(priceBasket1, 16);
            Assert.AreEqual(priceBasket2, 26);
            Assert.AreEqual(priceBasket3, 30);
            Assert.AreEqual(priceBasket4, (decimal)69.95);
        }


        [TestMethod]
        [ExpectedException(typeof(NotEnoughInventoryException))]
        public async Task UnsufficientInventory()
        {
            string[] basket = Regex.Split("Isaac Asimov - Robot series, Isaac Asimov - Robot series", Utils.Constants.BookBasketEntriesSplitRegex);

            await _inventoryManagerService.CalculateBookBasketPrice(basket);
        }
    }
}
