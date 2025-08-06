using AuthService.Models;

namespace AuthService.Services;

public class UserService
{
    private readonly List<User> _users = new();

    public void AddUser(User user) => _users.Add(user);

    public User? GetUser(string username) =>
        _users.FirstOrDefault(u => u.Username == username);
}
