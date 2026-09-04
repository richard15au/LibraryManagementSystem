using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // HOME PAGE
        // =========================================================

        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            // -----------------------------------------------------
            // New Arrivals
            // -----------------------------------------------------

            var newArrivals = await _context.Books
                .Include(b => b.Genre)
                .Include(b => b.BookAuthors)
                    .ThenInclude(ba => ba.Author)
                .AsNoTracking()
                .OrderByDescending(b => b.DateAdded)
                .Take(5)
                .ToListAsync();


            // -----------------------------------------------------
            // Most Borrowed Books
            // -----------------------------------------------------

            var mostBorrowed = await _context.Books
                .Include(b => b.Genre)
                .Include(b => b.BookAuthors)
                    .ThenInclude(ba => ba.Author)
                .Include(b => b.Borrowings)
                .AsNoTracking()
                .OrderByDescending(b => b.Borrowings.Count)
                .Take(5)
                .ToListAsync();


            // -----------------------------------------------------
            // Currently Available Books
            // -----------------------------------------------------

            var availableBooks = await _context.Books
                .Include(b => b.Genre)
                .Include(b => b.BookAuthors)
                    .ThenInclude(ba => ba.Author)
                .AsNoTracking()
                .Where(b =>
                    b.AvailabilityStatus == "Available")
                .OrderByDescending(b => b.DateAdded)
                .Take(5)
                .ToListAsync();


            // -----------------------------------------------------
            // Recommendations
            //
            // Currently based on available books with
            // borrowing activity.
            // -----------------------------------------------------

            var recommendations = await _context.Books
                .Include(b => b.Genre)
                .Include(b => b.BookAuthors)
                    .ThenInclude(ba => ba.Author)
                .Include(b => b.Borrowings)
                .AsNoTracking()
                .Where(b =>
                    b.AvailabilityStatus == "Available")
                .OrderByDescending(b => b.Borrowings.Count)
                .ThenByDescending(b => b.DateAdded)
                .Take(5)
                .ToListAsync();


            // -----------------------------------------------------
            // Group 5 Details
            // Richard is listed first as requested.
            // -----------------------------------------------------

            var groupMembers = new List<GroupMemberViewModel>
            {
                new GroupMemberViewModel
                {
                    StudentId = "20030988",
                    FullName = "Mr Richard Maceda VITUG"
                },

                new GroupMemberViewModel
                {
                    StudentId = "20038874",
                    FullName = "Mr Prakash RAUT"
                },

                new GroupMemberViewModel
                {
                    StudentId = "20032961",
                    FullName = "Mr Nikesh BASYAL"
                },

                new GroupMemberViewModel
                {
                    StudentId = "20024730",
                    FullName = "Mr Dipesh BHANDARI"
                }
            };


            // -----------------------------------------------------
            // Create Home ViewModel
            // -----------------------------------------------------

            var model = new HomeViewModel
            {
                NewArrivals = newArrivals,
                MostBorrowed = mostBorrowed,
                Recommendations = recommendations,
                AvailableBooks = availableBooks,
                GroupMembers = groupMembers
            };

            return View(model);
        }


        // =========================================================
        // ERROR
        // =========================================================

        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId =
                    System.Diagnostics.Activity
                        .Current?.Id
                    ?? HttpContext.TraceIdentifier
            });
        }
    }
}