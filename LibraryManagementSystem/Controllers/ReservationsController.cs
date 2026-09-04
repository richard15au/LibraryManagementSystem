using System.Security.Claims;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "Member,Librarian")]
    public class ReservationsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReservationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // GET: /Reservations
        // =========================================================

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (User.IsInRole("Librarian"))
            {
                var allReservations = await _context.Reservations
                    .Include(r => r.Book)
                    .Include(r => r.User)
                    .AsNoTracking()
                    .OrderByDescending(r => r.ReservationDate)
                    .ToListAsync();

                return View(allReservations);
            }

            var reservations = await _context.Reservations
                .Include(r => r.Book)
                .Include(r => r.User)
                .Where(r => r.UserId == userId)
                .AsNoTracking()
                .OrderByDescending(r => r.ReservationDate)
                .ToListAsync();

            return View(reservations);
        }


        // =========================================================
        // GET: /Reservations/Create?bookId=1
        // =========================================================

        [Authorize(Roles = "Member")]
        public async Task<IActionResult> Create(int? bookId)
        {
            if (bookId == null)
            {
                return NotFound();
            }

            var book = await _context.Books
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.BookId == bookId);

            if (book == null)
            {
                return NotFound();
            }

            return View(book);
        }


        // =========================================================
        // POST: /Reservations/Create
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> Create(int bookId)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }


            // -----------------------------------------------------
            // 1. Check that the book exists
            // -----------------------------------------------------

            var book = await _context.Books
                .FirstOrDefaultAsync(b => b.BookId == bookId);

            if (book == null)
            {
                return NotFound();
            }

            if (book.AvailabilityStatus == "Available")
            {
                TempData["ErrorMessage"] =
                    "This book is currently available. " +
                    "You can borrow it instead of reserving it.";

                return RedirectToAction(nameof(Index));
            }

            // -----------------------------------------------------
            // 2. Check for an existing active reservation
            // -----------------------------------------------------

            var existingReservation =
                await _context.Reservations
                    .AnyAsync(r =>
                        r.UserId == userId &&
                        r.BookId == bookId &&
                        r.Status == "Active");

            if (existingReservation)
            {
                TempData["ErrorMessage"] =
                    "You already have an active reservation " +
                    "for this book.";

                return RedirectToAction(nameof(Index));
            }


            // -----------------------------------------------------
            // 3. Create reservation
            // -----------------------------------------------------

            var reservation = new Reservation
            {
                UserId = userId,
                BookId = bookId,
                ReservationDate = DateTime.UtcNow,
                ExpiryDate = null,
                Status = "Active"
            };

            _context.Reservations.Add(reservation);

            await _context.SaveChangesAsync();


            // -----------------------------------------------------
            // 4. Success message
            // -----------------------------------------------------

            TempData["SuccessMessage"] =
                $"Reservation created for '{book.Title}'.";

            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // POST: /Reservations/Cancel/1
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Member,Librarian")]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            var reservation = await _context.Reservations
                .Include(r => r.Book)
                .FirstOrDefaultAsync(
                    r => r.ReservationId == id);

            if (reservation == null)
            {
                return NotFound();
            }


            // -----------------------------------------------------
            // Members can only cancel their own reservations.
            // Librarians can cancel any reservation.
            // -----------------------------------------------------

            if (!User.IsInRole("Librarian") &&
                reservation.UserId != userId)
            {
                return Forbid();
            }


            // -----------------------------------------------------
            // Already cancelled?
            // -----------------------------------------------------

            if (reservation.Status != "Active")
            {
                TempData["ErrorMessage"] =
                    "This reservation is no longer active.";

                return RedirectToAction(nameof(Index));
            }


            // -----------------------------------------------------
            // Cancel reservation
            // -----------------------------------------------------

            reservation.Status = "Cancelled";

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Reservation cancelled successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}