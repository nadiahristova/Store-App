using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryManager.Model
{
    [Table("BookCatalog")]
    public class BookCatalogEntry
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <value>The unique Name of the book.</value>
        [Required(ErrorMessage = "Book Title is required")]
        public string BookTitle { get; set; }

        public int AuthorId { get; set; }

        [ForeignKey("AuthorId")]
        [Required(ErrorMessage = "Author is required")]
        public Author Author { get; set; }

        public int CategoryId { get; set; }

        /// <value>Book category.</value>
        [Required(ErrorMessage = "Category is required")]
        [ForeignKey("CategoryId")]
        public Category Category { get; set; }

        /// <value>The price of an copy of the book.</value>
        public decimal Price { get; set; }

        /// <value>The Quantity of copy of the book in the catalog.</value>
        [Required(ErrorMessage = "Catalog Quantity is required")]
        public int Quantity { get; set; }
    }
}
