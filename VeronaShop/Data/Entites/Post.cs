using System.ComponentModel.DataAnnotations;

namespace VeronaShop.Data.Entites
{
    /// <summary>
    /// Blog post or content article.
    /// </summary>
    public class Post
    {
        [Key]
        public int Id { get; set; }

        /// <summary>Post title.</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>Short summary for listing pages.</summary>
        public string Summary { get; set; } = string.Empty;

        /// <summary>Full HTML or markdown content.</summary>
        public string Content { get; set; } = string.Empty;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
