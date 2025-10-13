using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace ToDoListProgram.Data
{
    public class TodoService
    {
        private readonly string _filePath;
        private readonly List<TodoItem> _tasks = new();
        public event Action? OnChange;

        public string FilePath => _filePath;
        public DateTime? LastSaved { get; private set; }

        public TodoService()
        {
            var dataDir = Path.Combine(AppContext.BaseDirectory, "Data");
            Directory.CreateDirectory(dataDir);
            _filePath = Path.Combine(dataDir, "tasks.json");

            Load();
        }

        public IEnumerable<TodoItem> GetAll() => _tasks;

        public IEnumerable<TodoItem> GetForUser(int userId)
            => _tasks.Where(t => t.UserId == userId).OrderBy(t => t.DueDate);

        public void Add(TodoItem item)
        {
            if (item.Id == 0)
                item.Id = _tasks.Count > 0 ? _tasks.Max(t => t.Id) + 1 : 1;

            _tasks.Add(item);
            Console.WriteLine($"[TodoService.Add] Task '{item.Title}' → UserId = {item.UserId}");
            Save();
        }

        public void Delete(int id)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == id);
            if (task != null)
            {
                _tasks.Remove(task);
                Save();
            }
        }

        public void Update(TodoItem item)
        {
            var existing = _tasks.FirstOrDefault(t => t.Id == item.Id);
            if (existing != null)
            {
                existing.Title = item.Title;
                existing.Category = item.Category;
                existing.Priority = item.Priority;
                existing.DueDate = item.DueDate;
                existing.IsCompleted = item.IsCompleted;
                Save();
            }
        }

        public void Reload()
        {
            _tasks.Clear();
            Load();
            NotifyStateChanged();
        }

        private void Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(_tasks, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_filePath, json);
                LastSaved = DateTime.UtcNow;
                Console.WriteLine($"[TodoService] Saved {_tasks.Count} items to {_filePath}");
                NotifyStateChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TodoService] Save failed: {ex.GetType().Name}: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        private void Load()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    var json = File.ReadAllText(_filePath);
                    var items = JsonSerializer.Deserialize<List<TodoItem>>(json);
                    if (items != null)
                        _tasks.AddRange(items);

                    Console.WriteLine($"[TodoService] Loaded {_tasks.Count} items from {_filePath}");
                }
                else
                {
                    Console.WriteLine($"[TodoService] No tasks.json found at {_filePath}. Starting empty.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TodoService] Load failed: {ex.GetType().Name}: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}
