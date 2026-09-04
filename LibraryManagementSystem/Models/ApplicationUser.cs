using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Address { get; set; }

        public DateTime DateRegistered { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        // Borrowing history
        public ICollection<Borrowing> Borrowings { get; set; } =
            new List<Borrowing>();

        // Reservations
        public ICollection<Reservation> Reservations { get; set; } =
            new List<Reservation>();

        // Reviews
        public ICollection<Review> Reviews { get; set; } =
            new List<Review>();
    }
}