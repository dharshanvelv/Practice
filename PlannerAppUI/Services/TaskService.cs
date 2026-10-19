using PlannerApplication.Models;
using PlannerApplication.Repositories;

namespace PlannerApplication.Services
{
    public class TaskService
    {
        private readonly TaskRepository taskRepository;

        public TaskService(TaskRepository taskRepository)
        {
            this.taskRepository = taskRepository;
        }

        public Result AddTask(string name, string desc, DateOnly startdate, DateOnly enddate, Guid projectId, Guid? userId = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Result.Failure("Task name cannot be empty");
            }

            if (enddate < startdate)
            {
                return Result.Failure("Target end date cannot be earlier than target start date");
            }

            if (projectId == Guid.Empty)
            {
                return Result.Failure("Invalid project id");
            }

            ToDoTask task = new ToDoTask(name.Trim(), desc?.Trim() ?? string.Empty, startdate, enddate, projectId, userId);
            taskRepository.AddTask(task);
            return Result.Success();
        }

        public List<ToDoTask> ViewTasks(Guid projectId)
        {
            List<ToDoTask> tasks = taskRepository.GetTasks();
            if (tasks == null)
            {
                return new List<ToDoTask>();
            }
            return tasks.FindAll(p => p.ProjectId == projectId);
        }

        public List<ToDoTask> ViewTasksByUser(Guid userId)
        {
            List<ToDoTask> tasks = taskRepository.GetTasks();
            if (tasks == null)
            {
                return new List<ToDoTask>();
            }
            return tasks.FindAll(p => p.UserId == userId);
        }

        public Result EditTask(Guid id, string name, string desc, DateOnly startdate, DateOnly enddate, Guid projectId, Guid? userId = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Result.Failure("Task name cannot be empty");
            if (enddate < startdate)
                return Result.Failure("Target end date cannot be earlier than target start date");

            ToDoTask? task = taskRepository.GetTasks().FirstOrDefault(t => t.Id == id && (t.ProjectId == projectId || projectId == Guid.Empty));
            if (task == null)
                return Result.Failure("Task not found");

            task.Name = name.Trim();
            task.Description = desc?.Trim() ?? string.Empty;
            task.TargetStartDate = startdate;
            task.TargetEndDate = enddate;
            task.UserId = userId;
            taskRepository.UpdateTask(task);
            return Result.Success();
        }

        public Result DeleteTask(Guid id, Guid? projectId = null)
        {
            ToDoTask? task = taskRepository.GetTasks().FirstOrDefault(t => t.Id == id);
            if (task == null)
                return Result.Failure("Task not found");

            if (projectId.HasValue && projectId.Value != Guid.Empty && task.ProjectId != projectId.Value)
                return Result.Failure("Task not found in project");

            taskRepository.DeleteTask(id);
            return Result.Success();
        }

        public Result UpdateTaskStatus(Guid id, TaskState status, Guid? projectId = null)
        {
            if (!Enum.IsDefined(status))
                return Result.Failure("Invalid task status");

            ToDoTask? task = taskRepository.GetTasks().FirstOrDefault(t => t.Id == id);
            if (task == null)
                return Result.Failure("Task not found");

            if (projectId.HasValue && projectId.Value != Guid.Empty && task.ProjectId != projectId.Value)
                return Result.Failure("Task not found in project");

            task.Status = status;
            if (!taskRepository.UpdateTask(task))
                return Result.Failure("Task not found");

            return Result.Success();
        }

        public TaskService() : this(new TaskRepository())
        {
        }

        public ToDoTask? GetTaskById(Guid id)
        {
            return taskRepository.GetTasks().FirstOrDefault(t => t.Id == id);
        }

        public Result AssignUser(Guid id, Guid? userId)
        {
            ToDoTask? task = taskRepository.GetTasks().FirstOrDefault(t => t.Id == id);
            if (task == null)
                return Result.Failure("Task not found");

            task.UserId = userId;
            if (!taskRepository.UpdateTask(task))
                return Result.Failure("Failed to assign user to task");

            return Result.Success();
        }

        public List<ToDoTask> SortTasks(Guid projectId, string sortBy = "StartDate")
        {
            var tasks = ViewTasks(projectId);
            return sortBy.ToLowerInvariant() switch
            {
                "enddate" => tasks.OrderBy(t => t.TargetEndDate).ToList(),
                "name" => tasks.OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase).ToList(),
                "status" => tasks.OrderBy(t => t.Status).ThenBy(t => t.TargetStartDate).ToList(),
                _ => tasks.OrderBy(t => t.TargetStartDate).ToList()
            };
        }

        public List<ToDoTask> FilterTasks(Guid projectId, string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return ViewTasks(projectId);

            string q = query.Trim();
            return ViewTasks(projectId).Where(task =>
                task.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                task.Description.Contains(q, StringComparison.OrdinalIgnoreCase)
            ).ToList();
        }

        public List<ToDoTask> FilterTasksByName(Guid projectId, string name)
        {
            return ViewTasks(projectId).Where(task => task.Name.Contains(name, StringComparison.OrdinalIgnoreCase)).ToList();
        }
    }
}