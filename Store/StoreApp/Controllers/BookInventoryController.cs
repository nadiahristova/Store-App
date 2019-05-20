using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using InventoryManager.Service.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StoreApp.Models;

namespace StoreApp.Controllers
{
    public class BookInventoryController : Controller
    {
        private IInventoryManagerService inventoryService;

        private const string UPLOAD_FILE_BAD_REQUEST_MSG = "Must specify valid file information";

        public BookInventoryController(IInventoryManagerService inventoryService)
        {
            this.inventoryService = inventoryService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadFile(IList<IFormFile> files)
        {
            if(files == null || files.Count == 0 || files.Any(file => file == null || file.Length == 0))
            {
                return BadRequest(UPLOAD_FILE_BAD_REQUEST_MSG);
            }

            try
            {
                var filesAsString = await Task.WhenAll(files.Select(file => ReadBookCatalogInfo(file)));

                await Task.WhenAll(filesAsString.Select(file => inventoryService.ImportBookInvenoryCatalog(file)));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            return Ok();
        }

        private async Task<string> ReadBookCatalogInfo(IFormFile file)
        {
            var bookCatalog = string.Empty;

            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                bookCatalog = await reader.ReadToEndAsync();
            }

            return bookCatalog;
        }

        [HttpGet]
        public async Task<IActionResult> GetBookQuantity(string bookIdentifier)
        {
            if (string.IsNullOrEmpty(bookIdentifier))
            {
                return BadRequest("Must specify identifier for book");
            }

            string[] bookInfo = Regex.Split(bookIdentifier, Utils.Constants.BookIdentifierSplitRegex);

            if (bookInfo.Length != 2 || bookInfo.Any(bi => string.IsNullOrWhiteSpace(bi)))
            {
                return BadRequest("Book identifier in not in the correct format");
            }

            int quantity = 0;

            try
            {
                quantity = await inventoryService.ReturnQuantityByBookTitle(bookInfo[1], bookInfo[0]);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            return Ok(new QuantityByBookTitle()
            {
                BookTitle = bookIdentifier,
                Quantity = quantity
            });
        }

        [HttpGet]
        public async Task<IActionResult> CalculateBasketPrice(string rawBookBasketList)
        {
            if (string.IsNullOrEmpty(rawBookBasketList))
            {
                return BadRequest("Must specify raw book basket list");
            }

            string[] basketBookList = Regex.Split(rawBookBasketList, Utils.Constants.BookBasketEntriesSplitRegex);

            if (basketBookList.Length == 0 || basketBookList.Any(bi => string.IsNullOrWhiteSpace(bi)))
            {
                return BadRequest("Basket list is not in the correct format");
            }

            decimal price = 0;

            try
            {
                price = await inventoryService.CalculateBookBasketPrice(basketBookList);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            return Ok(price);
        }
    }
}