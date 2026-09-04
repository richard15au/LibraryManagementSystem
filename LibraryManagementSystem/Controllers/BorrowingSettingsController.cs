using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "Librarian")]
    public class BorrowingSettingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BorrowingSettingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /BorrowingSettings
        public async Task<IActionResult> Index()
        {
            var settings = await _context.BorrowingSettings
                .Include(bs => bs.Library)
                .AsNoTracking()
                .ToListAsync();

            return View(settings);
        }

        // GET: /BorrowingSettings/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var settings = await _context.BorrowingSettings
                .Include(bs => bs.Library)
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    bs => bs.BorrowingSettingsId == id);

            if (settings == null)
            {
                return NotFound();
            }

            return View(settings);
        }

        // GET: /BorrowingSettings/Create
        public async Task<IActionResult> Create()
        {
            await LoadLibrariesAsync();

            return View();
        }

        // POST: /BorrowingSettings/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind(
                "LibraryId,LoanDurationDays,RenewalLimit," +
                "OverduePenalty,MaximumBorrowableItems")]
            BorrowingSettings settings)
        {
            // Check that the selected library exists.
            var libraryExists = await _context.Libraries
                .AnyAsync(l => l.LibraryId == settings.LibraryId);

            if (!libraryExists)
            {
                ModelState.AddModelError(
                    "LibraryId",
                    "The selected library does not exist.");
            }

            // A library may have only one settings record.
            var settingsAlreadyExist = await _context.BorrowingSettings
                .AnyAsync(bs => bs.LibraryId == settings.LibraryId);

            if (settingsAlreadyExist)
            {
                ModelState.AddModelError(
                    "LibraryId",
                    "This library already has borrowing settings.");
            }

            if (!ModelState.IsValid)
            {
                await LoadLibrariesAsync(settings.LibraryId);
                return View(settings);
            }

            _context.BorrowingSettings.Add(settings);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: /BorrowingSettings/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var settings = await _context.BorrowingSettings
                .FindAsync(id);

            if (settings == null)
            {
                return NotFound();
            }

            await LoadLibrariesAsync(settings.LibraryId);

            return View(settings);
        }

        // POST: /BorrowingSettings/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind(
                "BorrowingSettingsId,LibraryId,LoanDurationDays," +
                "RenewalLimit,OverduePenalty,MaximumBorrowableItems")]
            BorrowingSettings settings)
        {
            if (id != settings.BorrowingSettingsId)
            {
                return NotFound();
            }

            // Check that the selected library exists.
            var libraryExists = await _context.Libraries
                .AnyAsync(l => l.LibraryId == settings.LibraryId);

            if (!libraryExists)
            {
                ModelState.AddModelError(
                    "LibraryId",
                    "The selected library does not exist.");
            }

            // Make sure another settings record doesn't already
            // belong to this library.
            var duplicateSettings = await _context.BorrowingSettings
                .AnyAsync(bs =>
                    bs.LibraryId == settings.LibraryId &&
                    bs.BorrowingSettingsId != settings.BorrowingSettingsId);

            if (duplicateSettings)
            {
                ModelState.AddModelError(
                    "LibraryId",
                    "This library already has another borrowing settings record.");
            }

            if (!ModelState.IsValid)
            {
                await LoadLibrariesAsync(settings.LibraryId);
                return View(settings);
            }

            try
            {
                _context.BorrowingSettings.Update(settings);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BorrowingSettingsExists(
                    settings.BorrowingSettingsId))
                {
                    return NotFound();
                }

                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /BorrowingSettings/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var settings = await _context.BorrowingSettings
                .Include(bs => bs.Library)
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    bs => bs.BorrowingSettingsId == id);

            if (settings == null)
            {
                return NotFound();
            }

            return View(settings);
        }

        // POST: /BorrowingSettings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var settings = await _context.BorrowingSettings
                .FindAsync(id);

            if (settings == null)
            {
                return NotFound();
            }

            _context.BorrowingSettings.Remove(settings);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadLibrariesAsync(int? selectedLibraryId = null)
        {
            ViewData["LibraryId"] = new SelectList(
                await _context.Libraries
                    .AsNoTracking()
                    .OrderBy(l => l.Name)
                    .ToListAsync(),
                "LibraryId",
                "Name",
                selectedLibraryId);
        }

        private bool BorrowingSettingsExists(int id)
        {
            return _context.BorrowingSettings
                .Any(bs => bs.BorrowingSettingsId == id);
        }
    }
}