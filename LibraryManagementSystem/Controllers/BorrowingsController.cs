using System.Security.Claims;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "Member,Librarian")]
    public class BorrowingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BorrowingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // GET: /Borrowings
        // Display borrowing records
        // =========================================================

        public async Task<IActionResult> Index()
        {
            var query = _context.Borrowings
                .Include(b => b.Book)
                .Include(b => b.User)
                .Include(b => b.Fine)
                .AsNoTracking();

            // Members can only see their own borrowing history.
            if (User.IsInRole("Member"))
            {
                var userId = User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

                query = query.Where(b => b.UserId == userId);
            }

            // Librarians can see all borrowing records.

            var borrowings = await query
                .OrderByDescending(b => b.BorrowDate)
                .ToListAsync();

            return View(borrowings);
        }


        // =========================================================
        // GET: /Borrowings/Borrow
        // Display available books
        // =========================================================

        public async Task<IActionResult> Borrow()
        {
            var books = await _context.Books
                .AsNoTracking()
                .Where(b => b.AvailabilityStatus == "Available")
                .OrderBy(b => b.Title)
                .Select(b => new
                {
                    b.BookId,
                    b.Title,
                    b.ISBN
                })
                .ToListAsync();

            var model = new BorrowBookViewModel
            {
                Books = books.Select(b => new SelectListItem
                {
                    Value = b.BookId.ToString(),
                    Text = $"{b.Title} ({b.ISBN})"
                })
            };

            return View(model);
        }


        // =========================================================
        // POST: /Borrowings/Borrow
        // Create a borrowing
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Borrow(BorrowBookViewModel model)
        {
            // -----------------------------------------------------
            // 1. Get the currently logged-in user's Identity ID
            // -----------------------------------------------------

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }


            // -----------------------------------------------------
            // 2. Find the current user in the database
            // -----------------------------------------------------

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound("User account could not be found.");
            }


            // -----------------------------------------------------
            // 3. Check whether the account is active
            // -----------------------------------------------------

            if (!user.IsActive)
            {
                ModelState.AddModelError(
                    "",
                    "Your account is inactive and cannot borrow books.");

                await LoadAvailableBooksAsync(model);

                return View(model);
            }


            // -----------------------------------------------------
            // 4. Validate the selected BookId
            // -----------------------------------------------------

            if (!ModelState.IsValid)
            {
                await LoadAvailableBooksAsync(model);

                return View(model);
            }


            // -----------------------------------------------------
            // 5. Find the selected book
            // -----------------------------------------------------

            var book = await _context.Books
                .FirstOrDefaultAsync(b => b.BookId == model.BookId);

            if (book == null)
            {
                ModelState.AddModelError(
                    "BookId",
                    "The selected book could not be found.");

                await LoadAvailableBooksAsync(model);

                return View(model);
            }


            // -----------------------------------------------------
            // 6. Check whether the book is still available
            // -----------------------------------------------------
            // This check happens again during POST because another
            // user could have borrowed the book after the GET page
            // was displayed.
            // -----------------------------------------------------

            if (book.AvailabilityStatus != "Available")
            {
                ModelState.AddModelError(
                    "BookId",
                    "This book is no longer available.");

                await LoadAvailableBooksAsync(model);

                return View(model);
            }


            // -----------------------------------------------------
            // 7. Find the library
            // -----------------------------------------------------
            // For the current system we have one configured library.
            // We use its borrowing settings to calculate the loan.
            // -----------------------------------------------------

            var library = await _context.Libraries
                .Include(l => l.BorrowingSettings)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (library == null)
            {
                ModelState.AddModelError(
                    "",
                    "No library has been configured.");

                await LoadAvailableBooksAsync(model);

                return View(model);
            }


            // -----------------------------------------------------
            // 8. Make sure borrowing settings exist
            // -----------------------------------------------------

            var settings = library.BorrowingSettings;

            if (settings == null)
            {
                ModelState.AddModelError(
                    "",
                    "Borrowing settings have not been configured.");

                await LoadAvailableBooksAsync(model);

                return View(model);
            }


            // -----------------------------------------------------
            // 9. Count the user's active borrowings
            // -----------------------------------------------------

            var activeBorrowingCount = await _context.Borrowings
                .CountAsync(b =>
                    b.UserId == userId &&
                    b.ReturnDate == null &&
                    b.Status != "Returned");


            // -----------------------------------------------------
            // 10. Check maximum borrowing limit
            // -----------------------------------------------------

            if (activeBorrowingCount >= settings.MaximumBorrowableItems)
            {
                ModelState.AddModelError(
                    "",
                    $"You have reached the maximum borrowing limit " +
                    $"of {settings.MaximumBorrowableItems} books.");

                await LoadAvailableBooksAsync(model);

                return View(model);
            }


            // -----------------------------------------------------
            // 11. Calculate borrowing dates
            // -----------------------------------------------------

            var borrowDate = DateTime.UtcNow;

            var dueDate = borrowDate.AddDays(
                settings.LoanDurationDays);


            // -----------------------------------------------------
            // 12. Create the Borrowing entity
            // -----------------------------------------------------

            var borrowing = new Borrowing
            {
                UserId = userId,
                BookId = book.BookId,
                BorrowDate = borrowDate,
                DueDate = dueDate,
                ReturnDate = null,
                Status = "Borrowed",
                RenewalCount = 0
            };


            // -----------------------------------------------------
            // 13. Add borrowing to database
            // -----------------------------------------------------

            _context.Borrowings.Add(borrowing);


            // -----------------------------------------------------
            // 14. Change book availability
            // -----------------------------------------------------

            book.AvailabilityStatus = "Borrowed";


            // -----------------------------------------------------
            // 15. Save everything
            // -----------------------------------------------------

            await _context.SaveChangesAsync();


            // -----------------------------------------------------
            // 16. Return to borrowing list
            // -----------------------------------------------------

            TempData["SuccessMessage"] =
                $"'{book.Title}' was borrowed successfully. " +
                $"It is due on {dueDate.ToLocalTime():dd/MM/yyyy}.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // GET: /Borrowings/Return/1
        // Display return confirmation
        // =========================================================

        [Authorize(Roles = "Member,Librarian")]
        public async Task<IActionResult> Return(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var borrowing = await _context.Borrowings
                .Include(b => b.Book)
                .Include(b => b.User)
                .Include(b => b.Fine)
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    b => b.BorrowingId == id);

            if (borrowing == null)
            {
                return NotFound();
            }

            // A returned borrowing cannot be returned again.
            if (borrowing.ReturnDate != null ||
                borrowing.Status == "Returned")
            {
                return BadRequest(
                    "This book has already been returned.");
            }

            return View(borrowing);
        }
        // =========================================================
        // POST: /Borrowings/Return/1
        // Process the book return
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Member,Librarian")]
        public async Task<IActionResult> Return(int id)
        {
            // -----------------------------------------------------
            // 1. Find the borrowing
            // -----------------------------------------------------

            var borrowing = await _context.Borrowings
                .Include(b => b.Book)
                .Include(b => b.User)
                .FirstOrDefaultAsync(
                    b => b.BorrowingId == id);

            if (borrowing == null)
            {
                return NotFound();
            }


            // -----------------------------------------------------
            // 2. Make sure it has not already been returned
            // -----------------------------------------------------

            if (borrowing.ReturnDate != null ||
                borrowing.Status == "Returned")
            {
                return BadRequest(
                    "This book has already been returned.");
            }


            // -----------------------------------------------------
            // 3. Find the library borrowing settings
            // -----------------------------------------------------

            var library = await _context.Libraries
                .Include(l => l.BorrowingSettings)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (library == null)
            {
                return BadRequest(
                    "No library has been configured.");
            }

            var settings = library.BorrowingSettings;

            if (settings == null)
            {
                return BadRequest(
                    "Borrowing settings have not been configured.");
            }


            // -----------------------------------------------------
            // 4. Set the return date
            // -----------------------------------------------------

            var returnDate = DateTime.UtcNow;

            borrowing.ReturnDate = returnDate;
            borrowing.Status = "Returned";


            // -----------------------------------------------------
            // 5. Calculate overdue days
            // -----------------------------------------------------

            var overdueDays = 0;

            if (returnDate > borrowing.DueDate)
            {
                overdueDays =
                    (returnDate.Date - borrowing.DueDate.Date).Days;
            }


            // -----------------------------------------------------
            // 6. Create a fine if the book is overdue
            // -----------------------------------------------------

            if (overdueDays > 0)
            {
                var fineAmount =
                    overdueDays * settings.OverduePenalty;

                var fine = new Fine
                {
                    BorrowingId = borrowing.BorrowingId,
                    Amount = fineAmount,
                    Reason = $"Book returned {overdueDays} day(s) late.",
                    IssuedDate = returnDate,
                    Status = "Unpaid"
                };

                _context.Fines.Add(fine);
            }


            // -----------------------------------------------------
            // 7. Make the book available again
            // -----------------------------------------------------

            if (borrowing.Book != null)
            {
                borrowing.Book.AvailabilityStatus = "Available";
            }


            // -----------------------------------------------------
            // 8. Save all changes
            // -----------------------------------------------------

            await _context.SaveChangesAsync();


            // -----------------------------------------------------
            // 9. Display success message
            // -----------------------------------------------------

            if (overdueDays > 0)
            {
                var fineAmount =
                    overdueDays * settings.OverduePenalty;

                TempData["SuccessMessage"] =
                    $"Book returned successfully. " +
                    $"Overdue by {overdueDays} day(s). " +
                    $"Fine: {fineAmount:C}";
            }
            else
            {
                TempData["SuccessMessage"] =
                    "Book returned successfully. No fine was charged.";
            }


            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // POST: /Borrowings/Renew/2
        // Renew a borrowed book
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Member,Librarian")]
        public async Task<IActionResult> Renew(int id)
        {
            // -----------------------------------------------------
            // 1. Get the logged-in user's ID
            // -----------------------------------------------------

            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }


            // -----------------------------------------------------
            // 2. Find the borrowing
            // -----------------------------------------------------

            var borrowing = await _context.Borrowings
                .Include(b => b.Book)
                .FirstOrDefaultAsync(
                    b => b.BorrowingId == id);

            if (borrowing == null)
            {
                return NotFound();
            }


            // -----------------------------------------------------
            // 3. Members can only renew their own borrowing.
            // Librarians can renew any borrowing.
            // -----------------------------------------------------

            if (!User.IsInRole("Librarian") &&
                borrowing.UserId != userId)
            {
                return Forbid();
            }


            // -----------------------------------------------------
            // 4. Make sure the book is currently borrowed
            // -----------------------------------------------------

            if (borrowing.Status != "Borrowed" ||
                borrowing.ReturnDate != null)
            {
                TempData["ErrorMessage"] =
                    "This borrowing cannot be renewed.";

                return RedirectToAction(nameof(Index));
            }


            // -----------------------------------------------------
            // 5. Get borrowing settings
            // -----------------------------------------------------

            var library = await _context.Libraries
                .Include(l => l.BorrowingSettings)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (library == null ||
                library.BorrowingSettings == null)
            {
                return BadRequest(
                    "Borrowing settings have not been configured.");
            }

            var settings = library.BorrowingSettings;


            // -----------------------------------------------------
            // 6. Check renewal limit
            // -----------------------------------------------------

            if (borrowing.RenewalCount >=
                settings.RenewalLimit)
            {
                TempData["ErrorMessage"] =
                    "The renewal limit for this borrowing has been reached.";

                return RedirectToAction(nameof(Index));
            }


            // -----------------------------------------------------
            // 7. Extend the due date
            // -----------------------------------------------------

            borrowing.DueDate =
                borrowing.DueDate.AddDays(
                    settings.LoanDurationDays);

            borrowing.RenewalCount++;


            // -----------------------------------------------------
            // 8. Save changes
            // -----------------------------------------------------

            await _context.SaveChangesAsync();


            // -----------------------------------------------------
            // 9. Success message
            // -----------------------------------------------------

            TempData["SuccessMessage"] =
                $"Book renewed successfully. " +
                $"New due date: {borrowing.DueDate:dd/MM/yyyy}.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // Helper: Load available books
        // =========================================================

        private async Task LoadAvailableBooksAsync(
            BorrowBookViewModel model)
        {
            var books = await _context.Books
                .AsNoTracking()
                .Where(b => b.AvailabilityStatus == "Available")
                .OrderBy(b => b.Title)
                .Select(b => new
                {
                    b.BookId,
                    b.Title,
                    b.ISBN
                })
                .ToListAsync();

            model.Books = books.Select(b => new SelectListItem
            {
                Value = b.BookId.ToString(),
                Text = $"{b.Title} ({b.ISBN})"
            });
        }
    }
}