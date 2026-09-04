using System.Security.Claims;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "Member,Librarian")]
    public class ReviewsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReviewsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // GET: /Reviews
        // =========================================================

        public async Task<IActionResult> Index()
        {
            var query = _context.Reviews
                .Include(r => r.Book)
                .Include(r => r.User)
                .AsNoTracking();

            if (User.IsInRole("Member"))
            {
                var userId = User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

                query = query.Where(r =>
                    r.UserId == userId);
            }

            var reviews = await query
                .OrderByDescending(r => r.ReviewDate)
                .ToListAsync();

            return View(reviews);
        }

        // =========================================================
        // GET: /Reviews/Create?bookId=1
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
                .FirstOrDefaultAsync(
                    b => b.BookId == bookId);

            if (book == null)
            {
                return NotFound();
            }

            return View(book);
        }


        // =========================================================
        // POST: /Reviews/Create
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> Create(
            int bookId,
            int rating,
            string? comment)
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
            // 2. Validate rating
            // -----------------------------------------------------

            if (rating < 1 || rating > 5)
            {
                ModelState.AddModelError(
                    "Rating",
                    "Rating must be between 1 and 5.");
            }


            // -----------------------------------------------------
            // 3. Find the book
            // -----------------------------------------------------

            var book = await _context.Books
                .FirstOrDefaultAsync(
                    b => b.BookId == bookId);

            if (book == null)
            {
                return NotFound();
            }


            // -----------------------------------------------------
            // 4. Check for an existing review
            // -----------------------------------------------------

            var alreadyReviewed =
                await _context.Reviews
                    .AnyAsync(r =>
                        r.UserId == userId &&
                        r.BookId == bookId);

            if (alreadyReviewed)
            {
                TempData["ErrorMessage"] =
                    "You have already reviewed this book.";

                return RedirectToAction(nameof(Index));
            }


            // -----------------------------------------------------
            // 5. Validate comment length
            // -----------------------------------------------------

            if (comment != null &&
                comment.Length > 2000)
            {
                ModelState.AddModelError(
                    "Comment",
                    "Comment cannot exceed 2000 characters.");
            }


            if (!ModelState.IsValid)
            {
                return View(book);
            }


            // -----------------------------------------------------
            // 6. Create the review
            // -----------------------------------------------------

            var review = new Review
            {
                UserId = userId,
                BookId = bookId,
                Rating = rating,
                Comment = comment,
                ReviewDate = DateTime.UtcNow,
                IsApproved = false
            };


            _context.Reviews.Add(review);

            await _context.SaveChangesAsync();


            // -----------------------------------------------------
            // 7. Success
            // -----------------------------------------------------

            TempData["SuccessMessage"] =
                $"Your review for '{book.Title}' was submitted.";

            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // POST: /Reviews/Delete/1
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Member,Librarian")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            var review = await _context.Reviews
                .FirstOrDefaultAsync(
                    r => r.ReviewId == id);

            if (review == null)
            {
                return NotFound();
            }


            // -----------------------------------------------------
            // Members can delete only their own review.
            // Librarians can delete any review.
            // -----------------------------------------------------

            if (!User.IsInRole("Librarian") &&
                review.UserId != userId)
            {
                return Forbid();
            }


            _context.Reviews.Remove(review);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Review deleted successfully.";

            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Librarian")]
        public async Task<IActionResult> Approve(int id)
        {
            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.ReviewId == id);

            if (review == null)
                return NotFound();

            if (review.IsApproved)
            {
                TempData["ErrorMessage"] = "This review has already been approved.";
                return RedirectToAction(nameof(Index));
            }

            review.IsApproved = true;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Review approved successfully.";

            return RedirectToAction(nameof(Index));
        }
    }

}