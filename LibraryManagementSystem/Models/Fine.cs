using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models
{
    public class Fine
    {
        public int FineId { get; set; }

        [Required]
        public int BorrowingId { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(250)]
        public string Reason { get; set; } = string.Empty;

        [Required]
        public DateTime IssuedDate { get; set; } = DateTime.UtcNow;

        public DateTime? PaidDate { get; set; }

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Unpaid";

        // Relationship
        public Borrowing? Borrowing { get; set; }
    }
}