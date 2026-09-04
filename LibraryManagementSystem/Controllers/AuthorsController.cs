
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Data;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "Librarian")]
    public class AuthorsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuthorsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: AUTHORS
        public async Task<IActionResult> Index()
        {
            return View(await _context.Authors.ToListAsync());
        }

        // GET: AUTHORS/Details/5
        public async Task<IActionResult> Details(int? authorid)
        {
            if (authorid == null)
            {
                return NotFound();
            }

            var author = await _context.Authors
                .FirstOrDefaultAsync(m => m.AuthorId == authorid);
            if (author == null)
            {
                return NotFound();
            }

            return View(author);
        }

        // GET: AUTHORS/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: AUTHORS/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AuthorId,FirstName,LastName,Biography,BookAuthors")] Author author)
        {
            if (ModelState.IsValid)
            {
                _context.Add(author);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(author);
        }

        // GET: AUTHORS/Edit/5
        public async Task<IActionResult> Edit(int? authorid)
        {
            if (authorid == null)
            {
                return NotFound();
            }

            var author = await _context.Authors.FindAsync(authorid);
            if (author == null)
            {
                return NotFound();
            }
            return View(author);
        }

        // POST: AUTHORS/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int? authorid, [Bind("AuthorId,FirstName,LastName,Biography,BookAuthors")] Author author)
        {
            if (authorid != author.AuthorId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(author);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AuthorExists(author.AuthorId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(author);
        }

        // GET: AUTHORS/Delete/5
        public async Task<IActionResult> Delete(int? authorid)
        {
            if (authorid == null)
            {
                return NotFound();
            }

            var author = await _context.Authors
                .FirstOrDefaultAsync(m => m.AuthorId == authorid);
            if (author == null)
            {
                return NotFound();
            }

            return View(author);
        }

        // POST: AUTHORS/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int? authorid)
        {
            var author = await _context.Authors.FindAsync(authorid);
            if (author != null)
            {
                _context.Authors.Remove(author);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool AuthorExists(int? authorid)
        {
            return _context.Authors.Any(e => e.AuthorId == authorid);
        }
    }
}
