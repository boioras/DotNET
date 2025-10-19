using System.Text.Json;
using System.Threading.Tasks;

namespace ToDoListProgram.Data
{
    public interface ITodoService
    {
        // get all tasks
        public IEnumerable<TodoItem> GetAll();

        // get tasks for a specific user
        public IEnumerable<TodoItem> GetForUser(int userId);

        // add a new task
        public void Add(TodoItem item);

        // delete a task by id
        public void Delete(int id);

        // update an existing task
        public void Update(TodoItem item);

        // reload tasks from file
        public void Reload();

        // save tasks when the task list changes
        public void Save();

        // load tasks from file
        public void Load();

        // notify subscribers when the task list changes
        public void NotifyStateChanged();

    }
}
