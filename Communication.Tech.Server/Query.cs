using Communication.Tech.Server.Models;

namespace Communication.Tech.Server;

public class Query
{
    public IEnumerable<Book> Books([Service] BookRepository repo) => repo.GetBooks();

    public Book? GetBook(int id, [Service] BookRepository repo) => repo.GetBook(id);
}