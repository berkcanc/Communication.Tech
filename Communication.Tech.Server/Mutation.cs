using Communication.Tech.Server.Models;

namespace Communication.Tech.Server;

public class Mutation
{
    public Book AddBook(string title, string author, [Service] BookRepository repo)
    {
        var book = new Book { Title = title, Author = author };
        return repo.AddBook(book);
    }
}