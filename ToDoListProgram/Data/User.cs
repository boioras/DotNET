using System.Collections.Generic;

namespace ToDoListProgram.Data;

public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    ///  "User" / "Admin" 
    public string Role { get; set; } = "User";
}

// Entry point for polymorphic design
// instead of using if/else statements to check the Role string.
//Pages and services work directly with <see cref="User"/> objects,
public abstract class User
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string Role { get; set; } = "User";
    public List<TodoItem> Tasks { get; set; } = new();

    protected User() { }

    protected User(UserDto dto)
    {
        Id = dto.Id;
        Username = dto.Username;
        Password = dto.Password;
        Role = dto.Role;
    }

    // check if user is an admin (polymorphic override)
    public virtual bool IsAdmin => false;

    ///action menu bound to this user (polymorphic property).
    public virtual IEnumerable<MenuAction> GetMenuActions() => new[]
    {
        new MenuAction("viewTasks", "My Tasks", "checklist")
    };

    public void ResetPassword(string newPwd) => Password = newPwd;

    public override string ToString() => $"{Id} | {Role} | {Username}";
}

//admin
public sealed class AdminUser : User
{
    public AdminUser() { Role = "Admin"; }  
    public AdminUser(UserDto dto) : base(dto) { }

    public override bool IsAdmin => true;

    public override IEnumerable<MenuAction> GetMenuActions() => new[]
    {
        new MenuAction("createUser", "Add New User", "user-plus"),
        new MenuAction("editUser",   "Edit User",    "user-pen"),
        new MenuAction("deleteUser", "Delete User",  "user-x"),
        new MenuAction("resetPwd",   "Reset Password","key")
    };
}

//user
public sealed class NormalUser : User
{
    public NormalUser() { Role = "User"; }  
    public NormalUser(UserDto dto) : base(dto) { }

    public override IEnumerable<MenuAction> GetMenuActions() => new[]
    {
        new MenuAction("viewTasks", "My Tasks", "checklist"),
        new MenuAction("profile",   "Profile",  "id-card")
    };
}

//maps a Role value to a specific subclass (polymorphic instantiation).
//converts a <see cref="UserDto"/> to a runtime <see cref="User"/>
public static class UserFactory
{
    public static User FromDto(UserDto dto)
        => (dto.Role ?? "User").Trim().Equals("Admin", System.StringComparison.OrdinalIgnoreCase)
           ? new AdminUser(dto)
           : new NormalUser(dto);

    public static UserDto ToDto(User u) => new()
    {
        Id = u.Id,
        Username = u.Username,
        Password = u.Password,
        Role = u.Role
    };
}

public readonly record struct MenuAction(string Key, string Label, string? Icon = null);
