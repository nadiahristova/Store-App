using System.Diagnostics;
using InventoryManager.Service.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StoreApp.Models;

namespace StoreApp.Controllers
{
    public class HomeController : Controller
    {
        private IInventoryManagerService inventoryService;

        public HomeController(IInventoryManagerService inventoryService)
        {
            this.inventoryService = inventoryService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {

            return View();
        }

        public IActionResult Contact()
        {

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
