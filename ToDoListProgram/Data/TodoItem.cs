namespace ToDoListProgram.Data
{
    public class TodoItem
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public bool IsCompleted { get; set; }
        public string? Category { get; set; } = "General";
        public string? Priority { get; set; } = "M"; // H, M, L
        public DateTime? DueDate { get; set; }
    }
}