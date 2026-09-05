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

            var borrowings = await query
                .OrderByDescending(b => b.BorrowDate)
                .ToListAsync();

            // Automatically calculate the displayed status.
            var today = DateTime.UtcNow.Date;

            foreach (var borrowing in borrowings)
            {
                // Returned always remains Returned.
                if (borrowing.ReturnDate.HasValue)
                {
                    borrowing.Status = "Returned";
                }
                // Active borrowing past its due date becomes Overdue.
                else if (borrowing.DueDate.Date < today)
                {
                    borrowing.Status = "Overdue";
                }
                // Active borrowing that is not overdue remains Borrowed.
                else
                {
                    borrowing.Status = "Borrowed";
                }
            }

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
        public async Task<IActionResult> Borrow(
            BorrowBookViewModel model)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }


            // Find current user
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound(
                    "User account could not be found.");
            }


            // Check account status
            if (!user.IsActive)
            {
                ModelState.AddModelError(
                    "",
                    "Your account is inactive and cannot borrow books.");

                await LoadAvailableBooksAsync(model);

                return View(model);
            }


            // Validate model
            if (!ModelState.IsValid)
            {
                await LoadAvailableBooksAsync(model);

                return View(model);
            }


            // Find book
            var book = await _context.Books
                .FirstOrDefaultAsync(
                    b => b.BookId == model.BookId);

            if (book == null)
            {
                ModelState.AddModelError(
                    "BookId",
                    "The selected book could not be found.");

                await LoadAvailableBooksAsync(model);

                return View(model);
            }


            // Check availability
            if (book.AvailabilityStatus != "Available")
            {
                ModelState.AddModelError(
                    "BookId",
                    "This book is no longer available.");

                await LoadAvailableBooksAsync(model);

                return View(model);
            }


            // Find library
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


            var settings = library.BorrowingSettings;

            if (settings == null)
            {
                ModelState.AddModelError(
                    "",
                    "Borrowing settings have not been configured.");

                await LoadAvailableBooksAsync(model);

                return View(model);
            }


            // Count active borrowings
            var activeBorrowingCount =
                await _context.Borrowings.CountAsync(
                    b =>
                        b.UserId == userId &&
                        b.ReturnDate == null &&
                        b.Status != "Returned");


            // Maximum borrowing limit
            if (activeBorrowingCount >=
                settings.MaximumBorrowableItems)
            {
                ModelState.AddModelError(
                    "",
                    $"You have reached the maximum borrowing " +
                    $"limit of {settings.MaximumBorrowableItems} books.");

                await LoadAvailableBooksAsync(model);

                return View(model);
            }


            // Calculate dates
            var borrowDate = DateTime.UtcNow;

            var dueDate = borrowDate.AddDays(
                settings.LoanDurationDays);


            // Create borrowing
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

            _context.Borrowings.Add(borrowing);


            // Update book availability
            book.AvailabilityStatus = "Borrowed";


            await _context.SaveChangesAsync();


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

            // Already returned?
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
            var borrowing = await _context.Borrowings
                .Include(b => b.Book)
                .Include(b => b.User)
                .FirstOrDefaultAsync(
                    b => b.BorrowingId == id);

            if (borrowing == null)
            {
                return NotFound();
            }


            // Already returned?
            if (borrowing.ReturnDate != null ||
                borrowing.Status == "Returned")
            {
                return BadRequest(
                    "This book has already been returned.");
            }


            // Find library settings
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


            // Set return date
            var returnDate = DateTime.UtcNow;

            borrowing.ReturnDate = returnDate;
            borrowing.Status = "Returned";


            // Calculate overdue days
            var overdueDays = 0;

            if (returnDate.Date > borrowing.DueDate.Date)
            {
                overdueDays =
                    (returnDate.Date -
                     borrowing.DueDate.Date).Days;
            }


            // Create fine if returned late
            if (overdueDays > 0)
            {
                var fineAmount =
                    overdueDays * settings.OverduePenalty;

                var fine = new Fine
                {
                    BorrowingId = borrowing.BorrowingId,
                    Amount = fineAmount,
                    Reason =
                        $"Book returned {overdueDays} day(s) late.",
                    IssuedDate = returnDate,
                    Status = "Unpaid",
                    PaidDate = null
                };

                _context.Fines.Add(fine);
            }


            // Make book available again
            if (borrowing.Book != null)
            {
                borrowing.Book.AvailabilityStatus =
                    "Available";
            }


            await _context.SaveChangesAsync();


            // Success message
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
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }


            // Find borrowing
            var borrowing = await _context.Borrowings
                .Include(b => b.Book)
                .FirstOrDefaultAsync(
                    b => b.BorrowingId == id);

            if (borrowing == null)
            {
                return NotFound();
            }


            // Members can only renew their own borrowing.
            // Librarians can renew any borrowing.
            if (!User.IsInRole("Librarian") &&
                borrowing.UserId != userId)
            {
                return Forbid();
            }


            // Do not allow overdue or returned books to renew.
            if (borrowing.Status != "Borrowed" ||
                borrowing.ReturnDate != null ||
                borrowing.DueDate.Date < DateTime.UtcNow.Date)
            {
                TempData["ErrorMessage"] =
                    "This borrowing cannot be renewed because " +
                    "it is overdue or has already been returned.";

                return RedirectToAction(nameof(Index));
            }


            // Get borrowing settings
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


            // Check renewal limit
            if (borrowing.RenewalCount >=
                settings.RenewalLimit)
            {
                TempData["ErrorMessage"] =
                    "The renewal limit for this borrowing " +
                    "has been reached.";

                return RedirectToAction(nameof(Index));
            }


            // Extend due date
            borrowing.DueDate =
                borrowing.DueDate.AddDays(
                    settings.LoanDurationDays);

            borrowing.RenewalCount++;


            await _context.SaveChangesAsync();


            TempData["SuccessMessage"] =
                $"Book renewed successfully. " +
                $"New due date: " +
                $"{borrowing.DueDate:dd/MM/yyyy}.";

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
                .Where(b =>
                    b.AvailabilityStatus == "Available")
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