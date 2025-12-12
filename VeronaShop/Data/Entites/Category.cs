using System.ComponentModel.DataAnnotations;

namespace VeronaShop.Data.Entites
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string Name { get; set; }

        [MaxLength(150)]
        public string Slug { get; set; }

        public string Description { get; set; }

        public int? ParentCategoryId { get; set; }
        public Category ParentCategory { get; set; }

        public virtual ICollection<Product> Products { get; set; }
    }
}
