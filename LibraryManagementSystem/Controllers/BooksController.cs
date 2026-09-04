using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "Member,Librarian")]
    public class BooksController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BooksController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // INDEX
        // Members and Librarians can browse books
        // =========================================================

        public async Task<IActionResult> Index()
        {
            var books = await _context.Books
                .Include(b => b.Genre)
                .Include(b => b.BookAuthors)
                    .ThenInclude(ba => ba.Author)
                .AsNoTracking()
                .ToListAsync();

            return View(books);
        }


        // =========================================================
        // DETAILS
        // Members and Librarians can view book details
        // =========================================================

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books
                .Include(b => b.Genre)
                .Include(b => b.BookAuthors)
                    .ThenInclude(ba => ba.Author)
                .Include(b => b.Reviews)
                    .ThenInclude(r => r.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.BookId == id);

            if (book == null)
            {
                return NotFound();
            }

            return View(book);
        }


        // =========================================================
        // CREATE - GET
        // Librarian only
        // =========================================================

        [Authorize(Roles = "Librarian")]
        public async Task<IActionResult> Create()
        {
            var viewModel = new BookFormViewModel
            {
                Genres = await _context.Genres
                    .AsNoTracking()
                    .OrderBy(g => g.Name)
                    .ToListAsync(),

                Authors = await _context.Authors
                    .AsNoTracking()
                    .OrderBy(a => a.LastName)
                    .ThenBy(a => a.FirstName)
                    .ToListAsync()
            };

            return View(viewModel);
        }


        // =========================================================
        // CREATE - POST
        // Librarian only
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Librarian")]
        public async Task<IActionResult> Create(
            BookFormViewModel model,
            IFormFile? coverImageFile)
        {
            // -----------------------------------------------------
            // Check ISBN uniqueness
            // -----------------------------------------------------

            if (await _context.Books.AnyAsync(
                b => b.ISBN == model.ISBN))
            {
                ModelState.AddModelError(
                    nameof(model.ISBN),
                    "A book with this ISBN already exists.");
            }


            // -----------------------------------------------------
            // Make sure selected Genre exists
            // -----------------------------------------------------

            if (!await _context.Genres.AnyAsync(
                g => g.GenreId == model.GenreId))
            {
                ModelState.AddModelError(
                    nameof(model.GenreId),
                    "Please select a valid genre.");
            }


            // -----------------------------------------------------
            // Make sure selected Authors exist
            // -----------------------------------------------------

            if (model.SelectedAuthorIds.Any())
            {
                var validAuthorCount = await _context.Authors
                    .CountAsync(a =>
                        model.SelectedAuthorIds
                            .Contains(a.AuthorId));

                if (validAuthorCount !=
                    model.SelectedAuthorIds.Distinct().Count())
                {
                    ModelState.AddModelError(
                        nameof(model.SelectedAuthorIds),
                        "One or more selected authors are invalid.");
                }
            }


            // -----------------------------------------------------
            // Handle cover image upload
            // -----------------------------------------------------

            if (coverImageFile != null &&
                coverImageFile.Length > 0)
            {
                var allowedExtensions = new[]
                {
                    ".jpg",
                    ".jpeg",
                    ".png",
                    ".gif",
                    ".webp"
                };

                var extension =
                    Path.GetExtension(
                        coverImageFile.FileName)
                    .ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError(
                        "CoverImage",
                        "Only JPG, JPEG, PNG, GIF, and WEBP images are allowed.");
                }
                else if (coverImageFile.Length >
                         5 * 1024 * 1024)
                {
                    ModelState.AddModelError(
                        "CoverImage",
                        "The image must be smaller than 5 MB.");
                }
                else
                {
                    var uploadsFolder = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        "uploads",
                        "books");

                    Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName =
                        Guid.NewGuid().ToString()
                        + extension;

                    var filePath = Path.Combine(
                        uploadsFolder,
                        uniqueFileName);

                    using (var stream = new FileStream(
                        filePath,
                        FileMode.Create))
                    {
                        await coverImageFile.CopyToAsync(stream);
                    }

                    model.CoverImage =
                        "/uploads/books/" +
                        uniqueFileName;
                }
            }


            // -----------------------------------------------------
            // Validate model
            // -----------------------------------------------------

            if (!ModelState.IsValid)
            {
                await LoadBookFormData(model);
                return View(model);
            }


            // -----------------------------------------------------
            // Create Book
            // -----------------------------------------------------

            var book = new Book
            {
                Title = model.Title,
                ISBN = model.ISBN,
                Summary = model.Summary,
                CoverImage = model.CoverImage,
                PublishedDate = model.PublishedDate,
                AvailabilityStatus =
                    model.AvailabilityStatus,
                DateAdded = DateTime.UtcNow,
                GenreId = model.GenreId
            };

            _context.Books.Add(book);

            await _context.SaveChangesAsync();


            // -----------------------------------------------------
            // Create BookAuthor records
            // -----------------------------------------------------

            foreach (var authorId in
                model.SelectedAuthorIds.Distinct())
            {
                _context.BookAuthors.Add(
                    new BookAuthor
                    {
                        BookId = book.BookId,
                        AuthorId = authorId
                    });
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Book created successfully.";

            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // EDIT - GET
        // Librarian only
        // =========================================================

        [Authorize(Roles = "Librarian")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books
                .Include(b => b.BookAuthors)
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    b => b.BookId == id);

            if (book == null)
            {
                return NotFound();
            }

            var model = new BookFormViewModel
            {
                BookId = book.BookId,
                Title = book.Title,
                ISBN = book.ISBN,
                Summary = book.Summary,
                CoverImage = book.CoverImage,
                PublishedDate = book.PublishedDate,
                AvailabilityStatus =
                    book.AvailabilityStatus,
                GenreId = book.GenreId,

                SelectedAuthorIds = book.BookAuthors
                    .Select(ba => ba.AuthorId)
                    .ToList()
            };

            await LoadBookFormData(model);

            return View(model);
        }


        // =========================================================
        // EDIT - POST
        // Librarian only
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Librarian")]
        public async Task<IActionResult> Edit(
            int id,
            BookFormViewModel model,
            IFormFile? coverImageFile)
        {
            if (id != model.BookId)
            {
                return NotFound();
            }


            // -----------------------------------------------------
            // Check ISBN uniqueness
            // -----------------------------------------------------

            if (await _context.Books
                .AnyAsync(b =>
                    b.ISBN == model.ISBN &&
                    b.BookId != id))
            {
                ModelState.AddModelError(
                    nameof(model.ISBN),
                    "A book with this ISBN already exists.");
            }


            // -----------------------------------------------------
            // Check Genre
            // -----------------------------------------------------

            if (!await _context.Genres
                .AnyAsync(g =>
                    g.GenreId == model.GenreId))
            {
                ModelState.AddModelError(
                    nameof(model.GenreId),
                    "Please select a valid genre.");
            }


            // -----------------------------------------------------
            // Check Authors
            // -----------------------------------------------------

            if (model.SelectedAuthorIds.Any())
            {
                var validAuthorCount =
                    await _context.Authors
                        .CountAsync(a =>
                            model.SelectedAuthorIds
                                .Contains(a.AuthorId));

                if (validAuthorCount !=
                    model.SelectedAuthorIds
                        .Distinct()
                        .Count())
                {
                    ModelState.AddModelError(
                        nameof(model.SelectedAuthorIds),
                        "One or more selected authors are invalid.");
                }
            }


            // -----------------------------------------------------
            // Find existing book
            // -----------------------------------------------------

            var book = await _context.Books
                .Include(b => b.BookAuthors)
                .FirstOrDefaultAsync(
                    b => b.BookId == id);

            if (book == null)
            {
                return NotFound();
            }


            // -----------------------------------------------------
            // Handle new cover image
            // -----------------------------------------------------

            if (coverImageFile != null &&
                coverImageFile.Length > 0)
            {
                var allowedExtensions = new[]
                {
                    ".jpg",
                    ".jpeg",
                    ".png",
                    ".gif",
                    ".webp"
                };

                var extension =
                    Path.GetExtension(
                        coverImageFile.FileName)
                    .ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError(
                        "CoverImage",
                        "Only JPG, JPEG, PNG, GIF, and WEBP images are allowed.");
                }
                else if (coverImageFile.Length >
                         5 * 1024 * 1024)
                {
                    ModelState.AddModelError(
                        "CoverImage",
                        "The image must be smaller than 5 MB.");
                }
                else
                {
                    var uploadsFolder = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        "uploads",
                        "books");

                    Directory.CreateDirectory(
                        uploadsFolder);

                    // Delete old image if it exists
                    if (!string.IsNullOrEmpty(
                        book.CoverImage))
                    {
                        var oldFileName =
                            Path.GetFileName(
                                book.CoverImage);

                        var oldFilePath =
                            Path.Combine(
                                uploadsFolder,
                                oldFileName);

                        if (System.IO.File.Exists(
                            oldFilePath))
                        {
                            System.IO.File.Delete(
                                oldFilePath);
                        }
                    }

                    var uniqueFileName =
                        Guid.NewGuid().ToString()
                        + extension;

                    var filePath =
                        Path.Combine(
                            uploadsFolder,
                            uniqueFileName);

                    using (var stream =
                        new FileStream(
                            filePath,
                            FileMode.Create))
                    {
                        await coverImageFile
                            .CopyToAsync(stream);
                    }

                    model.CoverImage =
                        "/uploads/books/" +
                        uniqueFileName;
                }
            }


            // -----------------------------------------------------
            // If validation failed
            // -----------------------------------------------------

            if (!ModelState.IsValid)
            {
                model.CoverImage =
                    book.CoverImage;

                await LoadBookFormData(model);
                return View(model);
            }


            // -----------------------------------------------------
            // Update Book
            // -----------------------------------------------------

            book.Title = model.Title;
            book.ISBN = model.ISBN;
            book.Summary = model.Summary;

            // Keep old image if no new image was uploaded
            if (!string.IsNullOrEmpty(model.CoverImage))
            {
                book.CoverImage = model.CoverImage;
            }

            book.PublishedDate =
                model.PublishedDate;

            book.AvailabilityStatus =
                model.AvailabilityStatus;

            book.GenreId =
                model.GenreId;


            // -----------------------------------------------------
            // Update BookAuthor relationships
            // -----------------------------------------------------

            _context.BookAuthors
                .RemoveRange(book.BookAuthors);

            foreach (var authorId in
                model.SelectedAuthorIds.Distinct())
            {
                book.BookAuthors.Add(
                    new BookAuthor
                    {
                        BookId = book.BookId,
                        AuthorId = authorId
                    });
            }


            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Book updated successfully.";

            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // DELETE - GET
        // Librarian only
        // =========================================================

        [Authorize(Roles = "Librarian")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books
                .Include(b => b.Genre)
                .Include(b => b.BookAuthors)
                    .ThenInclude(ba => ba.Author)
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    b => b.BookId == id);

            if (book == null)
            {
                return NotFound();
            }

            return View(book);
        }


        // =========================================================
        // DELETE - POST
        // Librarian only
        // =========================================================

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Librarian")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var book = await _context.Books
                .Include(b => b.BookAuthors)
                .FirstOrDefaultAsync(
                    b => b.BookId == id);

            if (book == null)
            {
                return NotFound();
            }

            _context.BookAuthors
                .RemoveRange(book.BookAuthors);

            // Delete cover image from wwwroot
            if (!string.IsNullOrEmpty(
                book.CoverImage))
            {
                var uploadsFolder = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "uploads",
                    "books");

                var fileName =
                    Path.GetFileName(
                        book.CoverImage);

                var filePath =
                    Path.Combine(
                        uploadsFolder,
                        fileName);

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            _context.Books.Remove(book);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] =
                    "This book cannot be deleted because it is being used by other library records.";

                return RedirectToAction(nameof(Index));
            }

            TempData["SuccessMessage"] =
                "Book deleted successfully.";

            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // HELPER
        // Loads Genres and Authors into the form
        // =========================================================

        private async Task LoadBookFormData(
            BookFormViewModel model)
        {
            model.Genres = await _context.Genres
                .AsNoTracking()
                .OrderBy(g => g.Name)
                .ToListAsync();

            model.Authors = await _context.Authors
                .AsNoTracking()
                .OrderBy(a => a.LastName)
                .ThenBy(a => a.FirstName)
                .ToListAsync();
        }
    }
}