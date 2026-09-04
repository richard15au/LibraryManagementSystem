using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models
{
    public class Author
    {
        public int AuthorId { get; set; }

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Biography { get; set; }

        // Many-to-many relationship with Book
        public ICollection<BookAuthor> BookAuthors { get; set; } = new List<BookAuthor>();
    }
}