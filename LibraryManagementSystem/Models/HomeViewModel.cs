using System.Collections.Generic;

namespace LibraryManagementSystem.Models
{
    public class HomeViewModel
    {
        public IEnumerable<Book> NewArrivals { get; set; }
            = new List<Book>();

        public IEnumerable<Book> MostBorrowed { get; set; }
            = new List<Book>();

        public IEnumerable<Book> Recommendations { get; set; }
            = new List<Book>();

        public IEnumerable<Book> AvailableBooks { get; set; }
            = new List<Book>();

        public List<GroupMemberViewModel> GroupMembers { get; set; }
            = new List<GroupMemberViewModel>();
    }

    public class GroupMemberViewModel
    {
        public string StudentId { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;
    }
}