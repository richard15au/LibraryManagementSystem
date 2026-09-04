using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models
{
    public class BookFormViewModel
    {
        public int BookId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string ISBN { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Summary { get; set; }

        [StringLength(500)]
        public string? CoverImage { get; set; }

        public DateTime? PublishedDate { get; set; }

        [Required]
        [StringLength(30)]
        public string AvailabilityStatus { get; set; } = "Available";

        [Required]
        public int GenreId { get; set; }

        // Authors selected by the librarian
        public List<int> SelectedAuthorIds { get; set; } = new();

        // Data used to populate the form
        public List<Genre> Genres { get; set; } = new();

        public List<Author> Authors { get; set; } = new();
    }
}