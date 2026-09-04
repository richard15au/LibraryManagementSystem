using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LibraryManagementSystem.ViewModels
{
    public class BorrowBookViewModel
    {
        [Required(ErrorMessage = "Please select a book.")]
        [Display(Name = "Book")]
        public int BookId { get; set; }

        public IEnumerable<SelectListItem>? Books { get; set; }
    }
}