using InventoryManager.Data;
using InventoryManager.Service.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using InventoryManager.Service.JSONImportDTOs.BookInventory;
using InventoryManager.Model;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using Utils;
using InventoryManager.Service.BindingModels;
using InventoryManager.Service.Exceptions;
using Microsoft.Extensions.Logging;

namespace InventoryManager.Service
{
    public class InventoryManagerService : IHostedService, IInventoryManagerService
    {
        private readonly IServiceScopeFactory scopeFactory;
        private readonly ILogger logger;

        public InventoryManagerService(IServiceScopeFactory scopeFactory, ILogger<InventoryManagerService> logger)
        {
            this.scopeFactory = scopeFactory;
            this.logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation($"{DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")} - {this.GetType().Name} started.");

            using (var scope = scopeFactory.CreateScope())
            using (var ctx = scope.ServiceProvider.GetRequiredService<InventoryManagerContext>())
            {
                ctx.Database.Migrate();
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation($"{DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")} - {this.GetType().Name} stopped.");

            return Task.CompletedTask;
        }

        public async Task<decimal> CalculateBookBasketPrice(params string[] bookTitlesInBasket)
        {
            bookTitlesInBasket.ToTrimmedLowerCaseValues();

            using (var scope = scopeFactory.CreateScope())
            using (var ctx = scope.ServiceProvider.GetRequiredService<InventoryManagerContext>())
            {
                IDictionary<int, int> bookToOrderCountMapper = await GetNumberOfOrdersByBookId(ctx, bookTitlesInBasket);
                var orderedBookIds = bookToOrderCountMapper.Keys.AsEnumerable();

                var basketPrice = (from book in ctx.BookCatalog
                            where orderedBookIds.Contains(book.Id)
                            group book by book.CategoryId into booksGroupedByCategory
                            join category in ctx.Categories on booksGroupedByCategory.Key equals category.Id
                            let categoryDiscount = booksGroupedByCategory.Count() > 1 ? category.Discount : 0
                            select booksGroupedByCategory
                                    .Sum(book => book.Price * bookToOrderCountMapper[book.Id] - book.Price * (decimal)categoryDiscount)
                            ).Sum();

                return basketPrice;
            }
        }

        public async Task<int> ReturnQuantityByBookTitle(string bookTitle, string authorFullName)
        {
            using (var scope = scopeFactory.CreateScope())
            using (var ctx = scope.ServiceProvider.GetRequiredService<InventoryManagerContext>())
            {
                var catalog = await ctx.BookCatalog
                    .SingleOrDefaultAsync(b => b.BookTitle.ToLower() == bookTitle.ToLower() && b.Author.FullName.ToLower() == authorFullName.ToLower());

                return catalog.Quantity;
            }
        }

        public async Task ImportBookInvenoryCatalog(string bookInventoryInfo)
        {
            try
            {
                var json = JObject.Parse(bookInventoryInfo);

                var categoriesParsed = Task.Run(()=> ExtractCollectionOfDTOs<CategoryDTO>(json, "Category"));
                var catalogsParsed = Task.Run(() => ExtractCollectionOfDTOs<CatalogDTO>(json, "Catalog"));

                await Task.WhenAll(categoriesParsed, catalogsParsed);

                await PushBookInventoryDataToDatabase(categoriesParsed.Result, catalogsParsed.Result);
            }
            catch (Exception ex)
            {
                string errMsg = $"Book catalog import not in a correct format.";

                logger.LogInformation(errMsg);

                throw new FormatException(errMsg);
            }
        }

        private async Task<IDictionary<int, int>> GetNumberOfOrdersByBookId(InventoryManagerContext ctx, string[] bookTitlesInBasket)
        {
            var unsufficientInventory = new List<BasicBookPurchaseInfo>();

            var booksGroupedByTitle = bookTitlesInBasket.GroupBy(bookTitle => bookTitle);

            var ordersByBookId = new Dictionary<int, int>();

            foreach (var bookGroup in booksGroupedByTitle)
            {
                (string bookAuthor, string bookTitle) = SplitBookIdentifierInfo(bookGroup.Key);
                var numOfRequestedBooks = bookGroup.Count();

                var bookDB = await ctx.BookCatalog.SingleOrDefaultAsync(b => b.Author.FullName.ToLower() == bookAuthor && b.BookTitle.ToLower() == bookTitle);

                if (bookDB == null || bookDB.Quantity < numOfRequestedBooks)
                {
                    unsufficientInventory.Add(new BasicBookPurchaseInfo()
                    {
                        Title = bookTitle,
                        Author = bookAuthor,
                        UnsufficientQuantity = bookDB == null ? numOfRequestedBooks : numOfRequestedBooks - bookDB.Quantity
                    });

                    if(ordersByBookId != null) ordersByBookId = null;
                }
                else if(ordersByBookId != null && !ordersByBookId.ContainsKey(bookDB.Id))
                {
                    ordersByBookId.Add(bookDB.Id, numOfRequestedBooks);
                }
            }

            if (unsufficientInventory.Any())
            {
                throw new NotEnoughInventoryException("Boom");
            }

            return ordersByBookId;
        }

        private async Task PushBookInventoryDataToDatabase(IList<CategoryDTO> categoriesParsed, IList<CatalogDTO> catalogsParsed)
        {
            using (var scope = scopeFactory.CreateScope())
            using (var ctx = scope.ServiceProvider.GetRequiredService<InventoryManagerContext>())
            {
                await AddOrUpdateNewCategories(ctx, categoriesParsed);

                await AddOrUpdateBookCatalogEntries(ctx, catalogsParsed);
            }
        }

        private async Task AddOrUpdateBookCatalogEntries(InventoryManagerContext ctx, IList<CatalogDTO> catalogsParsed)
        {
            foreach (var catalog in catalogsParsed)
            {
                (string authorFullName, string bookTitle) = SplitBookIdentifierInfo(catalog.Name);

                int authorDBId = await ReturnAuthorId(ctx, authorFullName);
                int categoryDBId = await ReturnCategoryId(ctx, catalog.Category);

                var bookDBEntry = await ctx.BookCatalog
                   .FirstOrDefaultAsync(bc => bc.BookTitle.ToLower() == bookTitle && bc.AuthorId == authorDBId);

                if (bookDBEntry == null) // Add new book catalog entry
                {
                    var newBookCatalogEntry = new BookCatalogEntry()
                    {
                        BookTitle = bookTitle,
                        AuthorId = authorDBId,
                        CategoryId = categoryDBId,
                        Price = catalog.Price,
                        Quantity = catalog.Quantity
                    };

                    await ctx.BookCatalog.AddAsync(newBookCatalogEntry);
                }
                else // Update book catalog entry if need be
                {
                    if (bookDBEntry.CategoryId != categoryDBId)
                    {
                        bookDBEntry.CategoryId = categoryDBId;
                    }

                    if (bookDBEntry.Price != catalog.Price)
                    {
                        bookDBEntry.Price = catalog.Price;
                    }

                    if (bookDBEntry.Quantity != catalog.Quantity)
                    {
                        bookDBEntry.Quantity = catalog.Quantity;
                    }
                }
            }

            await ctx.SaveChangesAsync();
        }

        private async Task<int> ReturnCategoryId(InventoryManagerContext ctx, string category)
        {
            // Default Discount = 0
            var categoryDBEntry = await AddOrUpdateCategory(ctx, new CategoryDTO() { Name = category }, false);

            await ctx.SaveChangesAsync();

            return categoryDBEntry.Id;
        }

        private async Task<int> ReturnAuthorId(InventoryManagerContext ctx, string authorFullName)
        {
            var authorDBEntry = await ctx.Authors
                  .FirstOrDefaultAsync(a => a.FullName.ToLower() == authorFullName.ToLower());

            if (authorDBEntry == null)
            {
                authorDBEntry = new Author()
                {
                    FullName = authorFullName
                };

                await ctx.Authors.AddAsync(authorDBEntry);
            }

            await ctx.SaveChangesAsync();

            return authorDBEntry.Id;
        }

        private (string, string) SplitBookIdentifierInfo(string bookIdentifier)
        {
            string[] bookInfo = Regex.Split(bookIdentifier, Utils.Constants.BookIdentifierSplitRegex);

            if (bookInfo.Length != 2 || string.IsNullOrEmpty(bookInfo[0]) || string.IsNullOrEmpty(bookInfo[1]))
            {
                throw new ArgumentException("Book identifier not in the correct format");
            }

            return (bookInfo[0], bookInfo[1]);
        }

        private async Task AddOrUpdateNewCategories(InventoryManagerContext ctx, IList<CategoryDTO> categoriesParsed)
        {
            // ctx is not thread safe we cannot use Parallel.Foreach
            foreach (var category in categoriesParsed)
            {
                await AddOrUpdateCategory(ctx, category);
            }

            await ctx.SaveChangesAsync();
        }

        private async Task<Category> AddOrUpdateCategory(InventoryManagerContext ctx, CategoryDTO category, bool allowUpdate = true)
        {
            var categoryDB = await ctx.Categories
                .FirstOrDefaultAsync(c => c.Name.ToLower() == category.Name.ToLower());

            if (categoryDB == null)
            {
                categoryDB = new Category()
                {
                    Name = category.Name,
                    Discount = category.Discount
                };

                await ctx.Categories.AddAsync(categoryDB);
            }
            else if(allowUpdate)// Update Category info
            {
                if (categoryDB.Discount != category.Discount)
                {
                    categoryDB.Discount = category.Discount;
                }
            }

            return categoryDB;
        }

        private static IList<T> ExtractCollectionOfDTOs<T>(JObject jsonObj, string propertyName) where T: class
        {
            IList<T> parsedCollection = null;

            JArray collection = (JArray)jsonObj[propertyName];
            parsedCollection = collection.ToObject<IList<T>>();

            return parsedCollection;
        }
    }
}
