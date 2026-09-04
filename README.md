\# Library Management System



\## ICT272 Web Design and Development - Assessment 3



A web-based Library Management System developed using ASP.NET Core MVC, C#, Entity Framework Core and SQL Server.



The system provides separate functionality for library members and librarians, including book browsing, borrowing, reservations, fines, reviews, library management and reporting.



\## Group 5



| Student ID | Full Name |

|---|---|

| 20030988 | Richard Vitug |

| 20038874 | Prakash RAUT |

| 20032961 | Nikesh BASYAL |



\## Features



\### Members



\- Secure registration and login

\- Browse available books

\- View book details, authors, genres, summaries, availability and cover images

\- Borrow books

\- Reserve unavailable books

\- View borrowing history

\- View due dates and borrowing statuses

\- Renew eligible borrowed books

\- View fines and payment status

\- Submit book ratings and reviews

\- Manage personal reservations and reviews



\### Librarians



\- Librarian dashboard

\- Manage books

\- Manage authors

\- Manage genres

\- Manage library profiles

\- Configure borrowing settings

\- Manage borrowing transactions

\- Manage reservations

\- Manage fines

\- Manage member reviews

\- View borrowing reports



\### Reports



\- Borrowing trends

\- Overdue books

\- Most active members

\- Most popular books



\## Technologies



\- ASP.NET Core MVC

\- C#

\- Entity Framework Core

\- Microsoft SQL Server / SQL Express

\- ASP.NET Core Identity

\- Bootstrap

\- HTML5

\- CSS3

\- JavaScript



\## Database



The application uses Entity Framework Core migrations to create and update the SQL Server database.



The project includes the required database migrations in:



`LibraryManagementSystem/Migrations/`



\## Running the Application



\### Requirements



\- Visual Studio with ASP.NET and web development support

\- .NET 10 SDK

\- SQL Server Express

\- SQL Server Express LocalDB or SQL Server Express instance



\### Setup



1\. Clone the repository.



2\. Open:



`LibraryManagementSystem.slnx`



3\. Check the database connection string in:



`LibraryManagementSystem/appsettings.json`



The default configuration uses SQL Server Express:



`Server=.\\SQLEXPRESS;Database=LibraryManagementSystem;Trusted\_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true`



4\. Open the Package Manager Console or terminal in the project directory.



5\. Apply the Entity Framework Core migrations:



```powershell

dotnet ef database update

