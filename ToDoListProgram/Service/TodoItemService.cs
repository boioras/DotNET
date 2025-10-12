using ToDoListProgram.Data;

namespace ToDoListProgram.Service
{
    public class TodoItemService
    {
        private readonly List<TodoItem> tasks = new();
        private int nextId = 1;

        //get the user's all tasks
        public List<TodoItem> GetAll(int userId) => tasks.Where(t => t.UserId == userId).ToList();

        //add a new task
        public void Add(int userId, string title, string category, string priority, DateTime? dueDate)
        {
            tasks.Add(new TodoItem
            {
                Id = nextId++,
                UserId = userId,
                Title = title,
                Category = category,
                Priority = priority,
                DueDate = dueDate
            });
        }

        //delete a task
        public void Delete(int userId, int taskId)
        {
            var task = tasks.FirstOrDefault(t => t.UserId == userId && t.Id == taskId);
            if (task != null) tasks.Remove(task);
        }

        //update a task
        public void Update(TodoItem updated)
        {
            var existing = tasks.FirstOrDefault(t => t.Id == updated.Id);
            if (existing != null)
            {
                existing.Title = updated.Title;
                existing.Category = updated.Category;
                existing.Priority = updated.Priority;
                existing.DueDate = updated.DueDate;
                existing.IsCompleted = updated.IsCompleted;
            }
        }
    }
}
