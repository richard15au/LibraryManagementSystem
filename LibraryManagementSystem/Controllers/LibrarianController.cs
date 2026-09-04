using LibraryManagementSystem.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "Librarian")]
    public class LibrarianController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LibrarianController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // LIBRARIAN DASHBOARD
        // =========================================================

        public async Task<IActionResult> Index()
        {
            // -----------------------------------------------------
            // Total books
            // -----------------------------------------------------

            var totalBooks = await _context.Books
                .CountAsync();


            // -----------------------------------------------------
            // Available books
            // -----------------------------------------------------

            var availableBooks = await _context.Books
                .CountAsync(b =>
                    b.AvailabilityStatus == "Available");


            // -----------------------------------------------------
            // Active borrowings
            // -----------------------------------------------------

            var activeBorrowings = await _context.Borrowings
                .CountAsync(b =>
                    b.Status == "Borrowed" &&
                    b.ReturnDate == null);


            // -----------------------------------------------------
            // Active reservations
            // -----------------------------------------------------

            var activeReservations = await _context.Reservations
                .CountAsync(r =>
                    r.Status == "Active");


            // -----------------------------------------------------
            // Overdue books
            // -----------------------------------------------------

            var today = DateTime.UtcNow;

            var overdueBooks = await _context.Borrowings
                .CountAsync(b =>
                    b.Status == "Borrowed" &&
                    b.ReturnDate == null &&
                    b.DueDate < today);


            // -----------------------------------------------------
            // Outstanding fines
            // -----------------------------------------------------

            var outstandingFines = await _context.Fines
                .Where(f =>
                    f.Status == "Unpaid")
                .SumAsync(f =>
                    (decimal?)f.Amount) ?? 0m;


            // -----------------------------------------------------
            // Recent borrowing transactions
            // -----------------------------------------------------

            var recentBorrowings = await _context.Borrowings
                .Include(b => b.Book)
                .Include(b => b.User)
                .AsNoTracking()
                .OrderByDescending(b => b.BorrowDate)
                .Take(10)
                .ToListAsync();


            // -----------------------------------------------------
            // Pass dashboard data to View
            // -----------------------------------------------------

            ViewBag.TotalBooks =
                totalBooks;

            ViewBag.AvailableBooks =
                availableBooks;

            ViewBag.ActiveBorrowings =
                activeBorrowings;

            ViewBag.ActiveReservations =
                activeReservations;

            ViewBag.OverdueBooks =
                overdueBooks;

            ViewBag.OutstandingFines =
                outstandingFines;

            ViewBag.RecentBorrowings =
                recentBorrowings;


            return View();
        }
    }
}