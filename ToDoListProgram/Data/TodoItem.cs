namespace ToDoListProgram.Data
{
    public class TodoItem
    {
        public int Id { get; set; } // auto-incremented primary key
        public int UserId { get; set; } // foreign key to User
        public string? Title { get; set; }
        public bool IsCompleted { get; set; }
        public string? Category { get; set; } = "General"; 
        public string? Priority { get; set; } = "M"; // High, Medium, Low. Default is Medium
        public DateTime? DueDate { get; set; }
    }
}
