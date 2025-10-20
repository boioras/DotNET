using System.Text.Json;
using ToDoListProgram.Data;

namespace ToDoListProgram.Service;

public class UserService
{
    private readonly List<User> users = new();   // 
    private int nextUserId = 1;
    private User? currentUser;

    public event Func<Task>? OnChange;

    private const string AdminRole = "Admin";
    private const string UserRole  = "User";
    private const string AdminUsername = "admin";
    private const string AdminDefaultPassword = "123";

    private readonly string usersStore =
        Path.Combine(AppContext.BaseDirectory, "Data", "users.json");

    public UserService()
    {
        LoadUsers();
        EnsureAdminExists(); // at least one admin
    }

    // create user(for admin)
    public bool CreateUser(string? username, string? password, string? role)
    {
        username = username?.Trim();
        password = password?.Trim();
        role     = string.IsNullOrWhiteSpace(role) ? UserRole : role.Trim();

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return false;

        // unique Username(case insensitive)
        if (users.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
            return false;

        // only can be either user or admin
        if (!role.Equals(AdminRole, StringComparison.OrdinalIgnoreCase) &&
            !role.Equals(UserRole,  StringComparison.OrdinalIgnoreCase))
        {
            role = UserRole;
        }

        var dto = new UserDto
        {
            Id = nextUserId++,
            Username = username!,
            Password = password!,
            Role = NormalizeRole(role)
        };

        users.Add(UserFactory.FromDto(dto));
        SaveUsers();
        NotifyStateChanged();
        return true;
    }

    // reset password(for admin)
    public bool ResetPassword(int userId, string? newPassword)
    {
        newPassword = newPassword?.Trim();
        if (string.IsNullOrWhiteSpace(newPassword)) return false;

        var u = users.FirstOrDefault(x => x.Id == userId);
        if (u is null) return false;

        u.ResetPassword(newPassword);
        SaveUsers();
        NotifyStateChanged();
        return true;
    }

    // Register (for guests, role is fixed as User)    
    public bool Register(string? username, string? password)
    {
        username = username?.Trim();
        password = password?.Trim();
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return false;

        if (users.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
            return false;

        var dto = new UserDto
        {
            Id = nextUserId++,
            Username = username!,
            Password = password!,
            Role = UserRole
        };

        users.Add(UserFactory.FromDto(dto));
        SaveUsers();
        NotifyStateChanged();
        return true;
    }

    // login
    public bool Login(string? username, string? password)
    {
        username = username?.Trim();
        password = password?.Trim();

        var user = users.FirstOrDefault(u =>
            u.Username.Equals(username, StringComparison.OrdinalIgnoreCase) &&
            u.Password == password);

        if (user is null) return false;

        currentUser = user;
        NotifyStateChanged();
        return true;
    }

    public bool IsLoggedIn() => currentUser is not null;

    public void Logout()
    {
        currentUser = null;
        NotifyStateChanged();
    }

    public User? GetCurrentUser() => currentUser;

    public List<User> GetAllUsers() => users.ToList();

    // Role determination — using polymorphism
    public bool IsAdmin()
    {
        var u = currentUser;
        if (u is null) return false;

        // Compatible admin
        return u.IsAdmin || u.Username.Equals(AdminUsername, StringComparison.OrdinalIgnoreCase);
    }

    public bool IsUser() => currentUser is not null && !IsAdmin();

    // update user(for admin)
    public bool UpdateUser(User updated)
    {
        var idx = users.FindIndex(u => u.Id == updated.Id);
        if (idx < 0) return false;

        // unique user name
        if (users.Any(u => u.Id != updated.Id &&
                           u.Username.Equals(updated.Username, StringComparison.OrdinalIgnoreCase)))
            return false;

        // at least one admin
        if (IsLastAdmin(updated.Id) &&
            !updated.IsAdmin &&
            !updated.Role.Equals(AdminRole, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Character can be switched, Admin <-> User
        users[idx] = ConvertRoleIfNeeded(updated);

        SaveUsers();
        NotifyStateChanged();
        return true;
    }

    // delete user
    public bool DeleteUser(int id)
    {
        // admin cannot delete himself
        if (currentUser?.Id == id) return false;

        // at least one admin
        if (IsLastAdmin(id)) return false;

        var removed = users.RemoveAll(u => u.Id == id) > 0;
        if (removed)
        {
            SaveUsers();
            NotifyStateChanged();
        }
        return removed;
    }

    private void EnsureAdminExists()
    {
        if (!users.Any(u => u.IsAdmin ||
                            u.Role.Equals(AdminRole, StringComparison.OrdinalIgnoreCase)))
        {
            var dto = new UserDto
            {
                Id = nextUserId++,
                Username = AdminUsername,
                Password = AdminDefaultPassword,
                Role = AdminRole
            };
            users.Add(UserFactory.FromDto(dto));
            SaveUsers();
        }

        if (users.Count > 0)
            nextUserId = Math.Max(nextUserId, users.Max(u => u.Id) + 1);
    }

    private bool IsLastAdmin(int targetUserId)
    {
        var adminIds = users
            .Where(u => u.IsAdmin || u.Role.Equals(AdminRole, StringComparison.OrdinalIgnoreCase))
            .Select(u => u.Id)
            .ToList();

        return adminIds.Count == 1 && adminIds[0] == targetUserId;
    }

    private void SaveUsers()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(usersStore)!);

        var dtos = users.Select(UserFactory.ToDto).ToList();

        var json = JsonSerializer.Serialize(dtos, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(usersStore, json);
    }

    private void LoadUsers()
    {
        try
        {
            if (!File.Exists(usersStore)) return;

            var json = File.ReadAllText(usersStore);
            var list = JsonSerializer.Deserialize<List<UserDto>>(json) ?? new List<UserDto>();

            users.Clear();
            users.AddRange(list.Select(UserFactory.FromDto));

            if (users.Count > 0)
                nextUserId = users.Max(u => u.Id) + 1;
        }
        catch
        {
            // EnsureAdminExist: fallback for exceptions such as file corruption
        }
    }

    private async void NotifyStateChanged()
    {
        if (OnChange is null) return;
        var handlers = OnChange.GetInvocationList().Cast<Func<Task>>();
        var tasks = handlers.Select(h =>
        {
            try { return h(); }
            catch { return Task.CompletedTask; }
        });
        await Task.WhenAll(tasks);
    }

    private static string NormalizeRole(string role)
        => role.Equals(AdminRole, StringComparison.OrdinalIgnoreCase) ? AdminRole : UserRole;

    private static User ConvertRoleIfNeeded(User u)
    {
        // If the type and role do not match, convert to the correct derived class
        var desiredRole = NormalizeRole(u.Role);
        var dto = UserFactory.ToDto(u);
        dto.Role = desiredRole;

        var target = UserFactory.FromDto(dto);

        // keep tasks
        if (u.Tasks is { Count: > 0 })
            target.Tasks.AddRange(u.Tasks);

        return target;
    }
}
