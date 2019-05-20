using Newtonsoft.Json;

namespace InventoryManager.Service.JSONImportDTOs.BookInventory
{
    class CategoryDTO
    {
        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("Discount")]
        public double Discount { get; set; }
    }
}
