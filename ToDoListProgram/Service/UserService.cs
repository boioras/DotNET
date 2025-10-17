using System.Text.Json;
using ToDoListProgram.Data;

namespace ToDoListProgram.Service
{
    public class UserService
    {
        private readonly List<User> users = new();
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
            EnsureAdminExists(); // at least have one admin
        }

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
        // loginin
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

        public bool IsLoggedIn() => currentUser != null;

        public void Logout()
        {
            currentUser = null;
            NotifyStateChanged();
        }

        public User? GetCurrentUser() => currentUser;

        public List<User> GetAllUsers() => users.ToList();

        // judge role
        public bool IsAdmin()
        {
            var u = currentUser;
            if (u is null) return false;
            return u.Role.Equals(AdminRole, StringComparison.OrdinalIgnoreCase)
                || u.Username.Equals(AdminUsername, StringComparison.OrdinalIgnoreCase);
        }
        public bool IsUser() => currentUser is not null && !IsAdmin();

        // admin page
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

        
        private void EnsureAdminExists()
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

        private bool IsLastAdmin(int targetUserId)
        {
            var adminIds = users.Where(u => u.Role.Equals(AdminRole, StringComparison.OrdinalIgnoreCase))
                                .Select(u => u.Id).ToList();
            return adminIds.Count == 1 && adminIds[0] == targetUserId;
        }

        private void SaveUsers()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(usersStore)!);
            var json = JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(usersStore, json);
        }

        private void LoadUsers()
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

        private async void NotifyStateChanged()
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
