using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryManager.Model
{
    [Table("Category")]
    public class Category
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required(ErrorMessage = "Category Name is required")]
        [DefaultValue(":\"")]
        /// <value>Category name.</value>
        public string Name { get; set; }

        [Required(ErrorMessage = "Category Discount required")]
        /// <value>The discount applies when buying multiple book of this category.</value>
        public double Discount { get; set; }
    }
}
