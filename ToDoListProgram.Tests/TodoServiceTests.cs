using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ToDoListProgram.Data;
using ToDoListProgram.Service;

namespace ToDoListProgram.Tests
{
    [TestFixture]
    internal class TodoServiceTests
    {
        private string _dataFile = default!; // temporary data file for testing
        private TodoService _svc = default!; // service under test

        [SetUp]
        public void Setup()
        {
            //clear the Data/tasks.json in the test run directory
            var dataDir = Path.Combine(AppContext.BaseDirectory, "Data");
            Directory.CreateDirectory(dataDir);
            _dataFile = Path.Combine(dataDir, "tasks.json");
            if (File.Exists(_dataFile)) // delete existing file
            {
                File.Delete(_dataFile);
            }
            _svc = new TodoService(); // initialize service
        }

        [Test]
        public void AddItem_AssignsUserId_And_CheckItemExisted()  // test adding a todo item
        {
            // Arrange
            var todoItem = new TodoItem { UserId = 1, Title = "Task A", Priority = "H", DueDate = DateTime.Today };

            // Act
            _svc.Add(todoItem);

            // Assert
            Assert.That(todoItem.Id, Is.GreaterThan(0)); 
            var tasks = _svc.GetForUser(1).ToList(); // get tasks for user whose id is 1
            Assert.That(tasks.Count(), Is.EqualTo(1)); // should have 1 task
            Assert.That(tasks[0].Title, Is.EqualTo("Task A")); // title should match
        }

        [Test]
        public void Update_ChangesFields() // test updating a todo item
        {
            var t = new TodoItem { UserId = 2, Title = "Old", Priority = "L" }; // create new todo item
            _svc.Add(t); // add to service

            t.Title = "New";
            t.Priority = "H";
            _svc.Update(t); // update the item

            var saved = _svc.GetForUser(2).Single(); 
            Assert.That(saved.Title, Is.EqualTo("New"));
            Assert.That(saved.Priority, Is.EqualTo("H"));
        }

        [Test]
        public void Delete_RemovesItem() // test deleting a todo item
        {
            var a = new TodoItem { UserId = 3, Title = "A" };
            var b = new TodoItem { UserId = 3, Title = "B" };
           
            _svc.Add(a); 
            _svc.Add(b);

            _svc.Delete(a.Id);  // delete item A

            var list = _svc.GetForUser(3).ToList();
            Assert.That(list.Count, Is.EqualTo(1));
            Assert.That(list[0].Title, Is.EqualTo("B"));
        }

        [Test]
        public void GetForUser_Orders_By_DueDate_Ascending() // test getting todo items for a user ordered by due date
        {
            _svc.Add(new TodoItem { UserId = 7, Title = "later", DueDate = DateTime.Today.AddDays(2) });
            _svc.Add(new TodoItem { UserId = 7, Title = "sooner", DueDate = DateTime.Today.AddDays(1) });
            _svc.Add(new TodoItem { UserId = 9, Title = "other user" });

            var list = _svc.GetForUser(7).Select(x => x.Title).ToArray(); // get titles for user 7
            Assert.That(list, Is.EqualTo(new[] { "sooner", "later" }).AsCollection); // should be ordered by due date
        }

        [Test]
        public async Task OnChange_ShouldTrigger_OnAddUpdateDelete() // test OnChange event 
        {
            var times = 0; // record the number of times the event is triggered.
            var tcs = new TaskCompletionSource(); // task completion source to signal test completion

            _svc.OnChange += () =>
            { // subscribe to OnChange event
                times++; // increment counter
                if (times >= 3) tcs.TrySetResult(); // signal completion after 3 events
                return Task.CompletedTask; 
            };

            var t = new TodoItem { UserId = 1, Title = "A" };
            _svc.Add(t);
            t.Title = "B"; 
            _svc.Update(t);
            _svc.Delete(t.Id);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2)); 
            await Task.WhenAny(tcs.Task, Task.Delay(Timeout.Infinite, cts.Token)); 

            Assert.That(times, Is.GreaterThanOrEqualTo(3)); // should have fired at least 3 times
        }

    }
}
