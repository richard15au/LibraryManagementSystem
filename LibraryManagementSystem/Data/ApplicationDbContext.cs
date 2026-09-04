using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Library
        public DbSet<Library> Libraries { get; set; }

        // Borrowing settings
        public DbSet<BorrowingSettings> BorrowingSettings { get; set; }

        // Books
        public DbSet<Book> Books { get; set; }

        // Genres
        public DbSet<Genre> Genres { get; set; }

        // Authors
        public DbSet<Author> Authors { get; set; }

        // Book-Author junction table
        public DbSet<BookAuthor> BookAuthors { get; set; }

        // Borrowing
        public DbSet<Borrowing> Borrowings { get; set; }

        // Reservations
        public DbSet<Reservation> Reservations { get; set; }

        // Fines
        public DbSet<Fine> Fines { get; set; }

        // Reviews
        public DbSet<Review> Reviews { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // IMPORTANT:
            // This keeps the ASP.NET Core Identity configuration.
            base.OnModelCreating(modelBuilder);


            // =========================================================
            // 1. LIBRARY -> BORROWING SETTINGS
            // Relationship: 1 : 1
            // =========================================================

            modelBuilder.Entity<Library>()
                .HasOne(l => l.BorrowingSettings)
                .WithOne(bs => bs.Library)
                .HasForeignKey<BorrowingSettings>(
                    bs => bs.LibraryId)
                .OnDelete(DeleteBehavior.Cascade);


            // =========================================================
            // 2. GENRE -> BOOK
            // Relationship: 1 : Many
            // =========================================================

            modelBuilder.Entity<Genre>()
                .HasMany(g => g.Books)
                .WithOne(b => b.Genre)
                .HasForeignKey(b => b.GenreId)
                .OnDelete(DeleteBehavior.Restrict);


            // =========================================================
            // 3. BOOK -> AUTHOR
            // Relationship: Many : Many
            // Through BookAuthor
            // =========================================================

            modelBuilder.Entity<BookAuthor>()
                .HasKey(ba => new
                {
                    ba.BookId,
                    ba.AuthorId
                });


            modelBuilder.Entity<BookAuthor>()
                .HasOne(ba => ba.Book)
                .WithMany(b => b.BookAuthors)
                .HasForeignKey(ba => ba.BookId)
                .OnDelete(DeleteBehavior.Cascade);


            modelBuilder.Entity<BookAuthor>()
                .HasOne(ba => ba.Author)
                .WithMany(a => a.BookAuthors)
                .HasForeignKey(ba => ba.AuthorId)
                .OnDelete(DeleteBehavior.Cascade);


            // =========================================================
            // 4. USER -> BORROWING
            // Relationship: 1 : Many
            // =========================================================

            modelBuilder.Entity<Borrowing>()
                .HasOne(b => b.User)
                .WithMany(u => u.Borrowings)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Restrict);


            // =========================================================
            // 5. BOOK -> BORROWING
            // Relationship: 1 : Many
            // =========================================================

            modelBuilder.Entity<Borrowing>()
                .HasOne(b => b.Book)
                .WithMany(book => book.Borrowings)
                .HasForeignKey(b => b.BookId)
                .OnDelete(DeleteBehavior.Restrict);


            // =========================================================
            // 6. USER -> RESERVATION
            // Relationship: 1 : Many
            // =========================================================

            modelBuilder.Entity<Reservation>()
                .HasOne(r => r.User)
                .WithMany(u => u.Reservations)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);


            // =========================================================
            // 7. BOOK -> RESERVATION
            // Relationship: 1 : Many
            // =========================================================

            modelBuilder.Entity<Reservation>()
                .HasOne(r => r.Book)
                .WithMany(b => b.Reservations)
                .HasForeignKey(r => r.BookId)
                .OnDelete(DeleteBehavior.Restrict);


            // =========================================================
            // 8. BORROWING -> FINE
            // Relationship: 1 : 0..1
            // =========================================================

            modelBuilder.Entity<Fine>()
                .HasOne(f => f.Borrowing)
                .WithOne(b => b.Fine)
                .HasForeignKey<Fine>(f => f.BorrowingId)
                .OnDelete(DeleteBehavior.Cascade);


            // Make sure one borrowing cannot have multiple fines
            modelBuilder.Entity<Fine>()
                .HasIndex(f => f.BorrowingId)
                .IsUnique();


            // =========================================================
            // 9. USER -> REVIEW
            // Relationship: 1 : Many
            // =========================================================

            modelBuilder.Entity<Review>()
                .HasOne(r => r.User)
                .WithMany(u => u.Reviews)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);


            // =========================================================
            // 10. BOOK -> REVIEW
            // Relationship: 1 : Many
            // =========================================================

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Book)
                .WithMany(b => b.Reviews)
                .HasForeignKey(r => r.BookId)
                .OnDelete(DeleteBehavior.Restrict);


            // One user can have only one review for a particular book
            modelBuilder.Entity<Review>()
                .HasIndex(r => new
                {
                    r.UserId,
                    r.BookId
                })
                .IsUnique();


            // =========================================================
            // 11. BOOK ISBN
            // ISBN must be unique
            // =========================================================

            modelBuilder.Entity<Book>()
                .HasIndex(b => b.ISBN)
                .IsUnique();


            // =========================================================
            // 12. GENRE NAME
            // Genre names must be unique
            // =========================================================

            modelBuilder.Entity<Genre>()
                .HasIndex(g => g.Name)
                .IsUnique();


            // =========================================================
            // 13. LIBRARY -> BORROWING SETTINGS
            // Only one settings record per library
            // =========================================================

            modelBuilder.Entity<BorrowingSettings>()
                .HasIndex(bs => bs.LibraryId)
                .IsUnique();


            // =========================================================
            // DECIMAL PRECISION
            // =========================================================

            modelBuilder.Entity<BorrowingSettings>()
                .Property(bs => bs.OverduePenalty)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Fine>()
                .Property(f => f.Amount)
                .HasPrecision(10, 2);
        }
    }
}