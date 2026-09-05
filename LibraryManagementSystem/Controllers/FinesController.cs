using System.Security.Claims;
using LibraryManagementSystem.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "Member,Librarian")]
    public class FinesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FinesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // GET: /Fines
        // Display fines
        // =========================================================

        public async Task<IActionResult> Index()
        {
            var query = _context.Fines
                .Include(f => f.Borrowing)
                    .ThenInclude(b => b!.Book)
                .Include(f => f.Borrowing)
                    .ThenInclude(b => b!.User)
                .AsNoTracking();

            // Members can only see their own fines.
            if (User.IsInRole("Member"))
            {
                var userId = User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

                query = query.Where(f =>
                    f.Borrowing!.UserId == userId);
            }

            var fines = await query
                .OrderByDescending(f => f.IssuedDate)
                .ToListAsync();

            return View(fines);
        }


        // =========================================================
        // POST: /Fines/MarkAsPaid/1
        // Mark a fine as paid
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Librarian")]
        public async Task<IActionResult> MarkAsPaid(int id)
        {
            var fine = await _context.Fines
                .FirstOrDefaultAsync(f => f.FineId == id);

            if (fine == null)
            {
                return NotFound();
            }

            // Prevent paying the same fine twice.
            if (fine.Status == "Paid")
            {
                TempData["ErrorMessage"] =
                    "This fine has already been marked as paid.";

                return RedirectToAction(nameof(Index));
            }

            // Update both status and payment date.
            fine.Status = "Paid";
            fine.PaidDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Fine marked as paid successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}