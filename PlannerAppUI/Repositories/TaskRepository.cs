using PlannerApplication.Models;

namespace PlannerApplication.Repositories
{
    public class TaskRepository
    {
        private List<ToDoTask> tasks = new();
        private TaskJson taskjson;

        public TaskRepository(TaskJson? taskJson = null)
        {
            this.taskjson = taskJson ?? new TaskJson();
            tasks = taskjson.GetTasks();
        }

        public void AddTask(ToDoTask task)
        {
            if (task.Id == Guid.Empty) task.Id = Guid.NewGuid();
            tasks = taskjson.GetTasks();
            tasks.Add(task);
            taskjson.SaveTasks(tasks);
        }

        public List<ToDoTask> GetTasks()
        {
            tasks = taskjson.GetTasks();
            return tasks;
        }

        public bool UpdateTask(ToDoTask task)
        {
            tasks = taskjson.GetTasks();
            ToDoTask? existingTask = tasks.FirstOrDefault(t => t.Id == task.Id);
            if (existingTask == null) return false;

            existingTask.Name = task.Name;
            existingTask.Description = task.Description;
            existingTask.TargetStartDate = task.TargetStartDate;
            existingTask.TargetEndDate = task.TargetEndDate;
            existingTask.Status = task.Status;
            existingTask.UserId = task.UserId;
            if (task.ProjectId != Guid.Empty)
            {
                existingTask.ProjectId = task.ProjectId;
            }
            taskjson.SaveTasks(tasks);
            return true;
        }

        public bool DeleteTask(Guid id)
        {
            tasks = taskjson.GetTasks();
            ToDoTask? task = tasks.FirstOrDefault(t => t.Id == id);
            if (task == null) return false;

            tasks.Remove(task);
            taskjson.SaveTasks(tasks);
            return true;
        }
    }
}