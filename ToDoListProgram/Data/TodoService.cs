using System.Collections.Generic;
using System.Linq;

namespace ToDoListProgram.Data
{
    public class TodoService
    {
        private List<TodoItem> items = new();
        private int nextId = 1;

        public List<TodoItem> GetAll() => items;

        public void Add(TodoItem item)
        {
            item.Id = nextId++;
            items.Add(item);
        }

        public void Update(TodoItem item)
        {
            var existing = items.FirstOrDefault(t => t.Id == item.Id);
            if (existing != null)
            {
                existing.Title = item.Title;
                existing.IsCompleted = item.IsCompleted;
                existing.Category = item.Category;
                existing.Priority = item.Priority;
                existing.DueDate = item.DueDate;
            }
        }

        public void Delete(int id)
        {
            items.RemoveAll(t => t.Id == id);
        }
    }
}