using System.ComponentModel.DataAnnotations;


namespace LibraryManagementSystem.Models
{
    public class Borrowing
    {
        public int BorrowingId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public int BookId { get; set; }

        [Required]
        public DateTime BorrowDate { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime DueDate { get; set; }

        public DateTime? ReturnDate { get; set; }

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Borrowed";

        [Required]
        [Range(0, 100)]
        public int RenewalCount { get; set; } = 0;

        // Relationships
        public Book? Book { get; set; }

        public ApplicationUser? User { get; set; }

        // A borrowing can have zero or one fine
        public Fine? Fine { get; set; }
    }
}