using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models
{
    public class BorrowingSettings
    {
        public int BorrowingSettingsId { get; set; }

        [Required]
        public int LibraryId { get; set; }

        [Required]
        [Range(1, 365)]
        public int LoanDurationDays { get; set; }

        [Required]
        [Range(0, 100)]
        public int RenewalLimit { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal OverduePenalty { get; set; }

        [Required]
        [Range(1, 100)]
        public int MaximumBorrowableItems { get; set; }

        // Relationship
        public Library? Library { get; set; }
    }
}