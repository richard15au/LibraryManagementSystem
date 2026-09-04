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

        // GET: /Libraries
        public async Task<IActionResult> Index()
        {
            var libraries = await _context.Libraries
                .AsNoTracking()
                .ToListAsync();

            return View(libraries);
        }

        // GET: /Libraries/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var library = await _context.Libraries
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.LibraryId == id);

            if (library == null)
            {
                return NotFound();
            }

            return View(library);
        }

        // GET: /Libraries/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Libraries/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Name,Location,OperatingHours,ContactDetails")]
            Library library)
        {
            if (!ModelState.IsValid)
            {
                return View(library);
            }

            _context.Libraries.Add(library);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: /Libraries/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var library = await _context.Libraries.FindAsync(id);

            if (library == null)
            {
                return NotFound();
            }

            return View(library);
        }

        // POST: /Libraries/Edit/5
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

            return RedirectToAction(nameof(Index));
        }

        // GET: /Libraries/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var library = await _context.Libraries
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.LibraryId == id);

            if (library == null)
            {
                return NotFound();
            }

            return View(library);
        }

        // POST: /Libraries/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var library = await _context.Libraries.FindAsync(id);

            if (library == null)
            {
                return NotFound();
            }

            _context.Libraries.Remove(library);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private bool LibraryExists(int id)
        {
            return _context.Libraries
                .Any(l => l.LibraryId == id);
        }
    }
}