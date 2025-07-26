using Communication.Tech.Server.Models;

namespace Communication.Tech.Server;

public class BookRepository
{
    private readonly List<Book> _books = new()
    {
        new Book { Id = 1, Title = "1984", Author = "George Orwell" },
        new Book { Id = 2, Title = "Brave New World", Author = "Aldous Huxley" }
    };

    public IEnumerable<Book> GetBooks() => _books;

    public Book? GetBook(int id) => _books.FirstOrDefault(b => b.Id == id);

    public Book AddBook(Book book)
    {
        book.Id = _books.Max(b => b.Id) + 1;
        _books.Add(book);
        return book;
    }
}