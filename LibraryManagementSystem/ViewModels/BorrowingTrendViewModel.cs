namespace LibraryManagementSystem.ViewModels
{
    public class BorrowingTrendViewModel
    {
        public int Year { get; set; }

        public int Month { get; set; }

        public string MonthName { get; set; } = string.Empty;

        public int BorrowingCount { get; set; }
    }
}