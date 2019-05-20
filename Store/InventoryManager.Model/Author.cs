using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryManager.Model
{
    [Table("Author")]
    public class Author
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <value>The unique Full Name of the author.</value>
        [Required(ErrorMessage = "Author Name is required")]
        public string FullName { get; set; }
    }
}
