using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models
{
    public class Library
    {
        public int LibraryId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Location { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string OperatingHours { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string ContactDetails { get; set; } = string.Empty;

        // One Library has one BorrowingSettings record
        public BorrowingSettings? BorrowingSettings { get; set; }
    }
}