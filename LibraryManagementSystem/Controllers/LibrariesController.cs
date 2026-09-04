using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "Librarian")]
    public class LibrariesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LibrariesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // GET: /Libraries
        // =========================================================

        public async Task<IActionResult> Index()
        {
            var library = await _context.Libraries
                .AsNoTracking()
                .FirstOrDefaultAsync();

            return View(library);
        }


        // =========================================================
        // GET: /Libraries/Details/5
        // =========================================================

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var library = await _context.Libraries
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    l => l.LibraryId == id);

            if (library == null)
            {
                return NotFound();
            }

            return View(library);
        }


        // =========================================================
        // GET: /Libraries/Edit/5
        // =========================================================

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var library = await _context.Libraries
                .FirstOrDefaultAsync(
                    l => l.LibraryId == id);

            if (library == null)
            {
                return NotFound();
            }

            return View(library);
        }


        // =========================================================
        // POST: /Libraries/Edit/5
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("LibraryId,Name,Location,OperatingHours,ContactDetails")]
            Library library)
        {
            if (id != library.LibraryId)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(library);
            }

            try
            {
                _context.Libraries.Update(library);

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!LibraryExists(library.LibraryId))
                {
                    return NotFound();
                }

                throw;
            }

            TempData["SuccessMessage"] =
                "Library profile updated successfully.";

            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // CHECK LIBRARY EXISTS
        // =========================================================

        private bool LibraryExists(int id)
        {
            return _context.Libraries
                .Any(l => l.LibraryId == id);
        }
    }
}