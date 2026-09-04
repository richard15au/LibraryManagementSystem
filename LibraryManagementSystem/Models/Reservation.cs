using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models
{
    public class Reservation
    {
        public int ReservationId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public int BookId { get; set; }

        [Required]
        public DateTime ReservationDate { get; set; } = DateTime.UtcNow;

        public DateTime? ExpiryDate { get; set; }

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Active";

        // Relationships
        public ApplicationUser? User { get; set; }

        public Book? Book { get; set; }
    }
}