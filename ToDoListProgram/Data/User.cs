namespace ToDoListProgram.Data
{
    public class User
    {
        public int Id { get; set; } // auto-incremented primary key
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string Role { get; set; } = "User"; //  "User", "Admin". Default is "User"

        public List<TodoItem> Tasks { get; set; } = new();
    }
}

