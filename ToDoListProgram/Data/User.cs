using System.Collections.Generic;

namespace ToDoListProgram.Data;

/// <summary>
/// 与 users.json 一一对应的“存储模型”（只承载 JSON 字段）。
/// —— 保证不改变磁盘上的数据结构 ——
/// </summary>
public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    /// <summary>原有： "User" / "Admin" / "Guest"（如有）</summary>
    public string Role { get; set; } = "User";
}

/// <summary>
/// 运行期“领域模型”的抽象基类（多态的入口）。
/// 页面和服务层以后尽量面向 <see cref="User"/> 编程，而不是 if/else 判断 Role 字符串。
/// </summary>
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

    /// <summary>是否为管理员（多态点）</summary>
    public virtual bool IsAdmin => false;

    /// <summary>UI 可直接绑定的“动作菜单”（多态点）</summary>
    public virtual IEnumerable<MenuAction> GetMenuActions() => new[]
    {
        new MenuAction("viewTasks", "My Tasks", "checklist")
    };

    public void ResetPassword(string newPwd) => Password = newPwd;

    public override string ToString() => $"{Id} | {Role} | {Username}";
}

/// <summary>管理员</summary>
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

/// <summary>普通用户</summary>
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

/// <summary>
/// 工厂：把 Role -> 具体子类（多态创建点）。
/// 保持与 JSON 的兼容：从 <see cref="UserDto"/> 转换为运行期的 <see cref="User"/>。
/// </summary>
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

/// <summary>页面可绑定的小型动作模型</summary>
public readonly record struct MenuAction(string Key, string Label, string? Icon = null);
