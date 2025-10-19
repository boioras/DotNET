using NUnit.Framework;
using ToDoListProgram.Service;
using System.Linq;

namespace ToDoListProgram.Tests
{
    [TestFixture]
    internal class UserServiceTests
    {
        private string _dataDir = default!;
        private string _usersFile = default!;
        private UserService _svc = default!; // service under test

        [SetUp]
        public void SetUp()
        {
            _dataDir = Path.Combine(AppContext.BaseDirectory, "Data");
            _usersFile = Path.Combine(_dataDir, "users.json"); 

            Directory.CreateDirectory(_dataDir);

            if (File.Exists(_usersFile))
            {
                File.Delete(_usersFile); // delete existing users file for clean test
            }

            _svc = new UserService(); 
        }

        [Test]
        public void Register_Login_Logout() // test user registration, login, and logout
        {
            Assert.That(_svc.Register("user1", "pwd"), Is.True); // successful registration
            Assert.That(_svc.Register("user1", "x"), Is.False); // duplicate username

            Assert.That(_svc.Login("user1", "pwd"), Is.True); // successful login
            Assert.That(_svc.IsLoggedIn(), Is.True); // should be logged in
            Assert.That(_svc.GetCurrentUser()!.Username, Is.EqualTo("user1")); // username should match

            _svc.Logout(); // logout
            Assert.That(_svc.IsLoggedIn(), Is.False); // should not be logged in
        }

        [Test]
        public void Admin_Create_Update_Reset_Delete() // test admin user management functions
        {
            Assert.That(_svc.Login("admin", "123"), Is.True);
            Assert.That(_svc.IsAdmin(), Is.True);

            // Create a new user
            Assert.That(_svc.CreateUser("user1", "111", "User"), Is.True);

            // Update the role
            var u = _svc.GetAllUsers().First(x => x.Username == "user1");
            u.Role = "Admin";
            Assert.That(_svc.UpdateUser(u), Is.True); 

            // Reset password
            Assert.That(_svc.ResetPassword(u.Id, "newpass"), Is.True);
            _svc.Logout();
            Assert.That(_svc.Login("user1", "newpass"), Is.True);

            // Switch back to admin, then delete user1
            _svc.Logout();
            Assert.That(_svc.Login("admin", "123"), Is.True);
            Assert.That(_svc.DeleteUser(u.Id), Is.True);
        }
    }
}
