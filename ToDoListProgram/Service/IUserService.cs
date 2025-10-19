using Microsoft.AspNetCore.Identity;
using System.Text.Json;
using ToDoListProgram.Data;

namespace ToDoListProgram.Service
{
    public interface IUserService
    {
        //create user
        public bool CreateUser(string? username, string? password, string? role);

        // reset password
        public bool ResetPassword(int userId, string? newPassword);

        // register
        public bool Register(string? username, string? password);

        // login
        public bool Login(string? username, string? password);

        // check login status
        public bool IsLoggedIn();

        // logout
        public void Logout();

        // get current user
        public User? GetCurrentUser();

        // get all users
        public List<User> GetAllUsers();

        // judge role if it's admin
        public bool IsAdmin();

        // judge role if it's user
        public bool IsUser();

        //update user
        public bool UpdateUser(User updated);

        // delete user
        public bool DeleteUser(int id);

        //ensure admin exists
        public void EnsureAdminExists();

        // check if it's the last admin
        public bool IsLastAdmin(int targetUserId);

        // save users
        public void SaveUsers();

        // load users
        public void LoadUsers();

    }
}
