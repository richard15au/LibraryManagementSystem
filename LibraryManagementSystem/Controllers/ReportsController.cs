using LibraryManagementSystem.Data;
using LibraryManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "Librarian")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // GET: /Reports
        // Borrowing Trends
        // =========================================================

        public async Task<IActionResult> Index()
        {
            var trends = await _context.Borrowings
                .AsNoTracking()
                .GroupBy(b => new
                {
                    b.BorrowDate.Year,
                    b.BorrowDate.Month
                })
                .Select(g => new BorrowingTrendViewModel
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    BorrowingCount = g.Count()
                })
                .OrderByDescending(x => x.Year)
                .ThenByDescending(x => x.Month)
                .ToListAsync();

            foreach (var trend in trends)
            {
                trend.MonthName = new DateTime(
                    trend.Year,
                    trend.Month,
                    1
                ).ToString("MMMM");
            }

            return View(trends);
        }

        // =========================================================
        // GET: /Reports/Overdue
        // Overdue Books Report
        // =========================================================

        public async Task<IActionResult> Overdue()
        {
            var today = DateTime.UtcNow;

            var overdueBorrowings = await _context.Borrowings
                .Include(b => b.Book)
                .Include(b => b.User)
                .Where(b =>
                    b.Status == "Borrowed" &&
                    b.DueDate < today)
                .AsNoTracking()
                .OrderBy(b => b.DueDate)
                .ToListAsync();

            return View(overdueBorrowings);
        }
        // =========================================================
        // GET: /Reports/ActiveMembers
        // Most Active Members Report
        // =========================================================

        public async Task<IActionResult> ActiveMembers()
        {
            var activeMembers = await _context.Borrowings
                .Include(b => b.User)
                .AsNoTracking()
                .GroupBy(b => new
                {
                    b.UserId,
                    b.User!.FirstName,
                    b.User.LastName,
                    b.User.Email
                })
                .Select(g => new
                {
                    MemberName =
                        (g.Key.FirstName + " " + g.Key.LastName).Trim(),

                    Email = g.Key.Email,

                    BorrowingCount = g.Count()
                })
                .OrderByDescending(x => x.BorrowingCount)
                .ToListAsync();

            return View(activeMembers);
        }
        // =========================================================
        // GET: /Reports/PopularBooks
        // Most Popular Books Report
        // =========================================================

        public async Task<IActionResult> PopularBooks()
        {
            var popularBooks = await _context.Borrowings
                .Include(b => b.Book)
                .AsNoTracking()
                .GroupBy(b => new
                {
                    b.BookId,
                    b.Book!.Title,
                    b.Book.ISBN
                })
                .Select(g => new
                {
                    BookTitle = g.Key.Title,
                    ISBN = g.Key.ISBN,
                    BorrowingCount = g.Count()
                })
                .OrderByDescending(x => x.BorrowingCount)
                .ThenBy(x => x.BookTitle)
                .ToListAsync();

            return View(popularBooks);
        }
    }
}