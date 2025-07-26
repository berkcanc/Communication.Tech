using communication_tech.Interfaces;

namespace communication_tech.Services;

public class MessageStoreService
{
    private const int Capacity = 100;
    private readonly LinkedList<string> _messages = [];
    private readonly object _lock = new();

    public void Add(string message)
    {
        lock (_lock)
        {
            if (_messages.Count >= Capacity)
            {
                _messages.RemoveFirst(); // remove oldest
            }
            _messages.AddLast(message);
        }
    }

    public List<string> GetAll()
    {
        lock (_lock)
        {
            return _messages.ToList();
        }
    }
}