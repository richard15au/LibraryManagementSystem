
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Data;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "Librarian")]
    public class GenresController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GenresController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: GENRES
        public async Task<IActionResult> Index()
        {
            return View(await _context.Genres.ToListAsync());
        }

        // GET: GENRES/Details/5
        public async Task<IActionResult> Details(int? genreid)
        {
            if (genreid == null)
            {
                return NotFound();
            }

            var genre = await _context.Genres
                .FirstOrDefaultAsync(m => m.GenreId == genreid);
            if (genre == null)
            {
                return NotFound();
            }

            return View(genre);
        }

        // GET: GENRES/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: GENRES/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("GenreId,Name,Description,Books")] Genre genre)
        {
            if (ModelState.IsValid)
            {
                _context.Add(genre);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(genre);
        }

        // GET: GENRES/Edit/5
        public async Task<IActionResult> Edit(int? genreid)
        {
            if (genreid == null)
            {
                return NotFound();
            }

            var genre = await _context.Genres.FindAsync(genreid);
            if (genre == null)
            {
                return NotFound();
            }
            return View(genre);
        }

        // POST: GENRES/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int? genreid, [Bind("GenreId,Name,Description,Books")] Genre genre)
        {
            if (genreid != genre.GenreId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(genre);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!GenreExists(genre.GenreId))
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
            return View(genre);
        }

        // GET: GENRES/Delete/5
        public async Task<IActionResult> Delete(int? genreid)
        {
            if (genreid == null)
            {
                return NotFound();
            }

            var genre = await _context.Genres
                .FirstOrDefaultAsync(m => m.GenreId == genreid);
            if (genre == null)
            {
                return NotFound();
            }

            return View(genre);
        }

        // POST: GENRES/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int? genreid)
        {
            var genre = await _context.Genres.FindAsync(genreid);
            if (genre != null)
            {
                _context.Genres.Remove(genre);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool GenreExists(int? genreid)
        {
            return _context.Genres.Any(e => e.GenreId == genreid);
        }
    }
}
