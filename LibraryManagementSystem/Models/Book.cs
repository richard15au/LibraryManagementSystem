using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models
{
    public class Book
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
        public DateTime DateAdded { get; set; } = DateTime.UtcNow;

        // Genre relationship
        [Required]
        public int GenreId { get; set; }

        public Genre? Genre { get; set; }

        // Author relationship
        public ICollection<BookAuthor> BookAuthors { get; set; } = new List<BookAuthor>();

        // Borrowing relationship
        public ICollection<Borrowing> Borrowings { get; set; } = new List<Borrowing>();

        // Reservation relationship
        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

        // Review relationship
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}