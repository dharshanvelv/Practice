using PlannerApplication.Models;
using PlannerApplication.Repositories;

namespace PlannerApplication.Services
{
    public class ProjectService
    {
        private readonly ProjectRepository projectRepository;
        private readonly UserRepository userRepository;
        private readonly TaskRepository taskRepository;

        public ProjectService(ProjectRepository projectRepository, UserRepository userRepository, TaskRepository taskRepository)
        {
            this.projectRepository = projectRepository;
            this.userRepository = userRepository;
            this.taskRepository = taskRepository;
        }

        public ProjectService() : this(new ProjectRepository(), new UserRepository(), new TaskRepository())
        {
        }

        public Result AddProject(Guid creatorUserId, string name, string description, string[]? usernames = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Result.Failure("Project name cannot be empty");
            }

            if (projectRepository.GetProjects().Any(p => string.Equals(p.ProjectName, name.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                return Result.Failure("A project with this name already exists");
            }

            List<Guid> memberIds = new List<Guid>();

            if (usernames != null && usernames.Length > 0)
            {
                var allUsers = userRepository.GetUsers();
                foreach (var rawName in usernames)
                {
                    string username = rawName.Trim();
                    if (string.IsNullOrWhiteSpace(username)) continue;

                    var user = allUsers.FirstOrDefault(u => string.Equals(u.Name, username, StringComparison.OrdinalIgnoreCase));
                    if (user != null && !memberIds.Contains(user.Id))
                    {
                        memberIds.Add(user.Id);
                    }
                }
            }

            if (creatorUserId != Guid.Empty && !memberIds.Contains(creatorUserId))
            {
                memberIds.Add(creatorUserId);
            }

            Project project = new Project
            {
                ProjectId = Guid.NewGuid(),
                ProjectName = name.Trim(),
                ProjectDescription = description?.Trim() ?? string.Empty,
                CreatedDate = DateOnly.FromDateTime(DateTime.Today),
                UserIds = memberIds
            };

            projectRepository.AddProject(project);
            return Result.Success();
        }

        public List<Project> ViewProjects(Guid? userId = null)
        {
            var projects = projectRepository.GetProjects();
            if (userId == null || userId == Guid.Empty)
            {
                return projects;
            }
            return projects.Where(p => p.UserIds == null || p.UserIds.Count == 0 || p.UserIds.Contains(userId.Value)).ToList();
        }

        public Project? GetProjectById(Guid projectId)
        {
            return projectRepository.GetProjects().FirstOrDefault(p => p.ProjectId == projectId || p.Id == projectId);
        }

        public Result EditProject(Guid projectId, Guid userId, string name, string description)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Result.Failure("Project name cannot be empty");
            }

            Project? project = GetProjectById(projectId);
            if (project == null)
            {
                return Result.Failure("Project not found");
            }

            if (HasUsersAssigned(project) && !IsUserMember(project, userId))
            {
                return Result.Failure("You do not have permission to edit this project");
            }

            project.ProjectName = name.Trim();
            project.ProjectDescription = description?.Trim() ?? string.Empty;
            projectRepository.UpdateProject(project);
            return Result.Success();
        }

        public Result CloseProject(Guid projectId, Guid userId)
        {
            Project? project = GetProjectById(projectId);
            if (project == null)
            {
                return Result.Failure("Project not found");
            }

            if (HasUsersAssigned(project) && !IsUserMember(project, userId))
            {
                return Result.Failure("You do not have permission to close this project");
            }

            var projectTasks = taskRepository.GetTasks().Where(t => t.ProjectId == projectId).ToList();
            foreach (var task in projectTasks)
            {
                taskRepository.DeleteTask(task.Id);
            }

            projectRepository.DeleteProject(projectId);
            return Result.Success();
        }

        public Result AddMembers(Guid projectId, Guid userId, string[] usernames)
        {
            Project? project = GetProjectById(projectId);
            if (project == null)
            {
                return Result.Failure("Project not found");
            }

            if (HasUsersAssigned(project) && !IsUserMember(project, userId))
            {
                return Result.Failure("You do not have permission to add members to this project");
            }

            var allUsers = userRepository.GetUsers();
            project.UserIds ??= new List<Guid>();

            foreach (var rawName in usernames)
            {
                string uname = rawName.Trim();
                if (string.IsNullOrWhiteSpace(uname)) continue;

                var user = allUsers.FirstOrDefault(u => string.Equals(u.Name, uname, StringComparison.OrdinalIgnoreCase));
                if (user != null && !project.UserIds.Contains(user.Id))
                {
                    project.UserIds.Add(user.Id);
                }
            }

            projectRepository.UpdateProject(project);
            return Result.Success();
        }

        public bool CanAccessProject(Guid projectId, Guid userId)
        {
            Project? project = GetProjectById(projectId);
            if (project == null) return false;
            if (!HasUsersAssigned(project)) return true;
            return IsUserMember(project, userId);
        }

        private static bool HasUsersAssigned(Project project)
        {
            return project.UserIds != null && project.UserIds.Count > 0;
        }

        private static bool IsUserMember(Project project, Guid userId)
        {
            return project.UserIds != null && project.UserIds.Contains(userId);
        }

        public int TaskCount(Guid projectId)
        {
            return taskRepository.GetTasks().Count(t => t.ProjectId == projectId);
        }

        public List<User> ViewUsers()
        {
            return userRepository.GetUsers();
        }

        public User? GetUserById(Guid userId)
        {
            return userRepository.GetUsers().FirstOrDefault(u => u.Id == userId);
        }

        public Result RemoveMember(Guid projectId, Guid userId, Guid memberIdToRemove)
        {
            Project? project = GetProjectById(projectId);
            if (project == null)
            {
                return Result.Failure("Project not found");
            }

            if (HasUsersAssigned(project) && !IsUserMember(project, userId))
            {
                return Result.Failure("You do not have permission to manage members of this project");
            }

            if (project.UserIds != null && project.UserIds.Contains(memberIdToRemove))
            {
                project.UserIds.Remove(memberIdToRemove);
                projectRepository.UpdateProject(project);
            }

            return Result.Success();
        }

        public (int Total, int NotStarted, int InProgress, int Completed) GetProjectTaskStats(Guid projectId)
        {
            var projectTasks = taskRepository.GetTasks().Where(t => t.ProjectId == projectId).ToList();
            int total = projectTasks.Count;
            int notStarted = projectTasks.Count(t => t.Status == TaskState.NotStarted);
            int inProgress = projectTasks.Count(t => t.Status == TaskState.InProgress);
            int completed = projectTasks.Count(t => t.Status == TaskState.Completed);
            return (total, notStarted, inProgress, completed);
        }

        public User? GetUserByName(string username)
        {
            return userRepository.GetUsers().FirstOrDefault(u => string.Equals(u.Name, username.Trim(), StringComparison.OrdinalIgnoreCase));
        }
    }
}
