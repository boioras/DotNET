using System.Text.Json;
using ToDoListProgram.Data;

namespace ToDoListProgram.Service
{
    public class UserService : IUserService
    {
        private readonly List<User> users = new();
        private int nextUserId = 1; // auto-increment user ID
        private User? currentUser;

        public event Func<Task>? OnChange; // event to notify subscribers of changes

        private const string AdminRole = "Admin";
        private const string UserRole  = "User";
        private const string AdminUsername = "admin";
        private const string AdminDefaultPassword = "123";

        private readonly string usersStore =
            Path.Combine(AppContext.BaseDirectory, "Data", "users.json"); // users storage file

        public UserService()
        {
            LoadUsers();
            EnsureAdminExists(); // at least have one admin
        }

        // create user
        public bool CreateUser(string? username, string? password, string? role)
        {
            username = username?.Trim();
            password = password?.Trim();
            role = string.IsNullOrWhiteSpace(role) ? "User" : role.Trim();

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return false;

            // username unique
            if (users.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
                return false;

            // only allow admin / user (can extend)
            if (!role.Equals("Admin", StringComparison.OrdinalIgnoreCase)
            && !role.Equals("User", StringComparison.OrdinalIgnoreCase))
                role = "User";

            users.Add(new User
            {
                Id = nextUserId++,
                Username = username!,
                Password = password!,
                Role = role
            });

            SaveUsers();
            NotifyStateChanged();
            return true;
        }

        // reset password
        public bool ResetPassword(int userId, string? newPassword)
        {
            newPassword = newPassword?.Trim();
            if (string.IsNullOrWhiteSpace(newPassword)) return false;

            var u = users.FirstOrDefault(x => x.Id == userId);
            if (u is null) return false;

            u.Password = newPassword;
            SaveUsers();
            NotifyStateChanged();
            return true;
        }

        // register
        public bool Register(string? username, string? password)
        {
            username = username?.Trim();
            password = password?.Trim();
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return false;

            if (users.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
                return false;

            users.Add(new User
            {
                Id = nextUserId++,
                Username = username!,
                Password = password!,
                Role = UserRole
            });
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

        // check login status
        public bool IsLoggedIn() => currentUser != null;

        // logout
        public void Logout()
        {
            currentUser = null;
            NotifyStateChanged();
        }

        // get current user
        public User? GetCurrentUser() => currentUser;

        // get all users
        public List<User> GetAllUsers() => users.ToList();

        // judge role if it's admin
        public bool IsAdmin()
        {
            var u = currentUser;
            if (u is null) return false;
            return u.Role.Equals(AdminRole, StringComparison.OrdinalIgnoreCase)
                || u.Username.Equals(AdminUsername, StringComparison.OrdinalIgnoreCase);
        }

        // judge role if it's user
        public bool IsUser() => currentUser is not null && !IsAdmin();

        // update user
        public bool UpdateUser(User updated)
        {
            var idx = users.FindIndex(u => u.Id == updated.Id);
            if (idx < 0) return false;

            // user unique
            if (users.Any(u => u.Id != updated.Id &&
                               u.Username.Equals(updated.Username, StringComparison.OrdinalIgnoreCase)))
                return false;

            // if only one admin left, it cannot be use as a user
            if (IsLastAdmin(updated.Id) &&
                !updated.Role.Equals(AdminRole, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            users[idx] = updated;
            SaveUsers();
            NotifyStateChanged();
            return true;
        }

        // delete user
        public bool DeleteUser(int id)
        {
            // admin cannot delete himself
            if (currentUser?.Id == id) return false;

            // cannot delete the last admin
            if (IsLastAdmin(id)) return false;

            var removed = users.RemoveAll(u => u.Id == id) > 0;
            if (removed)
            {
                SaveUsers();
                NotifyStateChanged();
            }
            return removed;
        }

        // ensure admin exists
        public void EnsureAdminExists()
        {
            if (!users.Any(u => u.Role.Equals(AdminRole, StringComparison.OrdinalIgnoreCase)))
            {
                users.Add(new User {
                    Id = nextUserId++,
                    Username = AdminUsername,
                    Password = AdminDefaultPassword,
                    Role = AdminRole,
                    // Name = "Administrator",    
                    // Email = "admin@example.com",
                    // Phone = ""
                });
                SaveUsers();
            }

            if (users.Count > 0)
                nextUserId = Math.Max(nextUserId, users.Max(u => u.Id) + 1);
        }

        // check if it's the last admin
        public bool IsLastAdmin(int targetUserId)
        {
            var adminIds = users.Where(u => u.Role.Equals(AdminRole, StringComparison.OrdinalIgnoreCase))
                                .Select(u => u.Id).ToList();
            return adminIds.Count == 1 && adminIds[0] == targetUserId;
        }

        // save users
        public void SaveUsers()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(usersStore)!);
            var json = JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(usersStore, json);
        }

        // load users
        public void LoadUsers()
        {
            try
            {
                if (!File.Exists(usersStore)) return;
                var json = File.ReadAllText(usersStore);
                var list = JsonSerializer.Deserialize<List<User>>(json) ?? new List<User>();
                users.Clear();
                users.AddRange(list);
                if (users.Count > 0) nextUserId = users.Max(u => u.Id) + 1;
            }
            catch
            {
                // if file corruption 
            }
        }

        // notify state changed
        public async void NotifyStateChanged()
        {
            if (OnChange == null) return;
            var handlers = OnChange.GetInvocationList().Cast<Func<Task>>();
            var tasks = handlers.Select(h =>
            {
                try { return h(); }
                catch { return Task.CompletedTask; }
            });
            await Task.WhenAll(tasks);
        }
    }
}
