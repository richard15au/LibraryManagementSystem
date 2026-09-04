using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models
{
    public class Review
    {
        public int ReviewId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public int BookId { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        [StringLength(2000)]
        public string? Comment { get; set; }

        [Required]
        public DateTime ReviewDate { get; set; } = DateTime.UtcNow;

        public bool IsApproved { get; set; } = true;

        // Relationships
        public ApplicationUser? User { get; set; }

        public Book? Book { get; set; }
    }
}