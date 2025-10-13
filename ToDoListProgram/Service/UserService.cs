using ToDoListProgram.Data;
using System.Linq;

namespace ToDoListProgram.Service
{
    public class UserService
    {
        private readonly List<User> users = new();
        private int nextUserId = 1;
        private User? currentUser;
        public event Func<Task>? OnChange; // event changed record
        public UserService()
        {
            // Initialize with a default admin user
            users.Add(new User { Id = nextUserId++, Username = "admin", Password = "123", Role = "Admin" });
        }

        // register
        public bool Register(string username, string password)
        {
            username = username?.Trim();
            password = password?.Trim();

            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password)) return false;

            if (users.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
                return false;

            users.Add(new User
            {
                Id = nextUserId++,
                Username = username,
                Password = password,           // store exactly as typed (or hash later)
                Role   = "User"
            });

            NotifyStateChanged();
            return true;
        }

        // login
        public bool Login(string username, string password)
        {
            username = username?.Trim();
            password = password?.Trim();

            var user = users.FirstOrDefault(u =>
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase) &&
                u.Password == password);

            if (user == null) return false;

            currentUser = user;
            NotifyStateChanged();
            return true;
        }

        // check if logged in
        public bool IsLoggedIn() => currentUser != null;

        // logout
        public void Logout()
        {
            currentUser = null; 
            NotifyStateChanged(); // notify to refresh state
        }

        // get current user
        public User? GetCurrentUser() => currentUser;

        // get all users (admin only)
        public List<User> GetAllUsers() => users;

        //notify to refresh state
        private async void NotifyStateChanged()
        {
            if(OnChange == null) return;

            var invocationList = OnChange.GetInvocationList().Cast<Func<Task>>().ToArray(); // get all subscribers
            var tasks = new List<Task>(invocationList.Length);

            foreach (var handler in invocationList)
            {
                try
                {
                    await handler();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in event handler: {ex.Message}");
                }
            }
        }
    }
}
