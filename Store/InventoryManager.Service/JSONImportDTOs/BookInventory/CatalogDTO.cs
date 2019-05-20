using Newtonsoft.Json;

namespace InventoryManager.Service.JSONImportDTOs.BookInventory
{
    class CatalogDTO
    {
        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("Category")]
        public string Category { get; set; }

        [JsonProperty("Price")]
        public decimal Price { get; set; }

        [JsonProperty("Quantity")]
        public int Quantity { get; set; }
    }
}
