using System;
using System.IO;
using System.Linq;
using PlannerApplication.Models;
using PlannerApplication.Repositories;
using PlannerApplication.Services;
using Xunit;

namespace PlannerApplication.Tests
{
    public class ProjectServiceTests : IDisposable
    {
        private readonly string _userFile;
        private readonly string _projectFile;
        private readonly string _taskFile;

        private readonly UserRepository _userRepo;
        private readonly ProjectRepository _projectRepo;
        private readonly TaskRepository _taskRepo;
        private readonly ProjectService _projectService;

        public ProjectServiceTests()
        {
            string id = Guid.NewGuid().ToString("N");
            _userFile = $"test_u_{id}.json";
            _projectFile = $"test_p_{id}.json";
            _taskFile = $"test_t_{id}.json";

            _userRepo = new UserRepository(new UserJson(_userFile));
            _projectRepo = new ProjectRepository(new ProjectJson(_projectFile));
            _taskRepo = new TaskRepository(new TaskJson(_taskFile));

            _projectService = new ProjectService(_projectRepo, _userRepo, _taskRepo);
        }

        public void Dispose()
        {
            if (File.Exists(_userFile)) File.Delete(_userFile);
            if (File.Exists(_projectFile)) File.Delete(_projectFile);
            if (File.Exists(_taskFile)) File.Delete(_taskFile);
        }

        [Fact]
        public void AddProject_ValidName_SavesProjectWithCreatorAsMember()
        {
            Guid creatorId = Guid.NewGuid();
            var result = _projectService.AddProject(creatorId, "Alpha Project", "First test project");

            Assert.True(result.IsSuccess);
            var projects = _projectService.ViewProjects();
            Assert.Single(projects);
            Assert.Equal("Alpha Project", projects[0].ProjectName);
            Assert.Contains(creatorId, projects[0].UserIds);
        }

        [Fact]
        public void AddProject_EmptyName_ReturnsFailure()
        {
            var result = _projectService.AddProject(Guid.NewGuid(), "   ", "Desc");
            Assert.False(result.IsSuccess);
            Assert.Equal("Project name cannot be empty", result.ErrorMessage);
        }

        [Fact]
        public void AddProject_DuplicateName_ReturnsFailure()
        {
            _projectService.AddProject(Guid.NewGuid(), "Beta", "Desc");
            var result = _projectService.AddProject(Guid.NewGuid(), "beta", "Another");

            Assert.False(result.IsSuccess);
            Assert.Contains("already exists", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void EditProject_UpdatesNameAndDescription()
        {
            Guid creatorId = Guid.NewGuid();
            _projectService.AddProject(creatorId, "Old Name", "Old Desc");
            var proj = _projectService.ViewProjects().First();

            var result = _projectService.EditProject(proj.ProjectId, creatorId, "New Name", "New Desc");
            Assert.True(result.IsSuccess);

            var updated = _projectService.GetProjectById(proj.ProjectId);
            Assert.NotNull(updated);
            Assert.Equal("New Name", updated.ProjectName);
            Assert.Equal("New Desc", updated.ProjectDescription);
        }

        [Fact]
        public void EditProject_PreservesUnchangedFields()
        {
            Guid creatorId = Guid.NewGuid();
            _projectService.AddProject(creatorId, "Static Name", "Original Desc");
            var proj = _projectService.ViewProjects().First();

            // Edit only description, keeping name unchanged
            var r1 = _projectService.EditProject(proj.ProjectId, creatorId, proj.ProjectName, "Updated Desc Only");
            Assert.True(r1.IsSuccess);

            var updated1 = _projectService.GetProjectById(proj.ProjectId);
            Assert.NotNull(updated1);
            Assert.Equal("Static Name", updated1.ProjectName);
            Assert.Equal("Updated Desc Only", updated1.ProjectDescription);

            // Edit only name, keeping description unchanged
            var r2 = _projectService.EditProject(proj.ProjectId, creatorId, "Updated Name Only", updated1.ProjectDescription);
            Assert.True(r2.IsSuccess);

            var updated2 = _projectService.GetProjectById(proj.ProjectId);
            Assert.NotNull(updated2);
            Assert.Equal("Updated Name Only", updated2.ProjectName);
            Assert.Equal("Updated Desc Only", updated2.ProjectDescription);
        }

        [Fact]
        public void EditProject_EmptyName_ReturnsFailure()
        {
            Guid creatorId = Guid.NewGuid();
            _projectService.AddProject(creatorId, "Initial Project", "Desc");
            var proj = _projectService.ViewProjects().First();

            var result = _projectService.EditProject(proj.ProjectId, creatorId, "   ", "Desc");
            Assert.False(result.IsSuccess);
            Assert.Equal("Project name cannot be empty", result.ErrorMessage);
        }

        [Fact]
        public void CloseProject_DeletesProjectAndCascadesTaskDeletion()
        {
            Guid creatorId = Guid.NewGuid();
            _projectService.AddProject(creatorId, "Project To Delete", "Desc");
            var proj = _projectService.ViewProjects().First();

            // Add a task in the project
            _taskRepo.AddTask(new ToDoTask("Subtask 1", "Desc", DateOnly.FromDateTime(DateTime.Today), DateOnly.FromDateTime(DateTime.Today).AddDays(1), proj.ProjectId));
            Assert.Single(_taskRepo.GetTasks(), t => t.ProjectId == proj.ProjectId);

            var result = _projectService.CloseProject(proj.ProjectId, creatorId);
            Assert.True(result.IsSuccess);

            // Verify project is removed
            Assert.Empty(_projectService.ViewProjects());

            // Verify cascaded tasks are removed
            Assert.DoesNotContain(_taskRepo.GetTasks(), t => t.ProjectId == proj.ProjectId);
        }

        [Fact]
        public void AddMembers_AddsValidUserIds()
        {
            User user1 = new User("colleague1", "pass");
            _userRepo.AddUser(user1);

            Guid creatorId = Guid.NewGuid();
            _projectService.AddProject(creatorId, "Team Project", "Desc");
            var proj = _projectService.ViewProjects().First();

            var result = _projectService.AddMembers(proj.ProjectId, creatorId, new[] { "colleague1" });
            Assert.True(result.IsSuccess);

            var updated = _projectService.GetProjectById(proj.ProjectId);
            Assert.NotNull(updated);
            Assert.Contains(user1.Id, updated.UserIds);
        }

        [Fact]
        public void RemoveMember_RemovesUserId()
        {
            User user1 = new User("member1", "pass");
            _userRepo.AddUser(user1);

            Guid creatorId = Guid.NewGuid();
            _projectService.AddProject(creatorId, "Collab Project", "Desc", new[] { "member1" });
            var proj = _projectService.ViewProjects().First();

            var result = _projectService.RemoveMember(proj.ProjectId, creatorId, user1.Id);
            Assert.True(result.IsSuccess);

            var updated = _projectService.GetProjectById(proj.ProjectId);
            Assert.NotNull(updated);
            Assert.DoesNotContain(user1.Id, updated.UserIds);
        }

        [Fact]
        public void GetProjectTaskStats_ReturnsAccurateCounts()
        {
            Guid creatorId = Guid.NewGuid();
            _projectService.AddProject(creatorId, "Stats Project", "Desc");
            var proj = _projectService.ViewProjects().First();

            DateOnly today = DateOnly.FromDateTime(DateTime.Today);
            _taskRepo.AddTask(new ToDoTask("T1", "", today, today, proj.ProjectId) { Status = TaskState.NotStarted });
            _taskRepo.AddTask(new ToDoTask("T2", "", today, today, proj.ProjectId) { Status = TaskState.InProgress });
            _taskRepo.AddTask(new ToDoTask("T3", "", today, today, proj.ProjectId) { Status = TaskState.Completed });
            _taskRepo.AddTask(new ToDoTask("T4", "", today, today, proj.ProjectId) { Status = TaskState.Completed });

            var stats = _projectService.GetProjectTaskStats(proj.ProjectId);
            Assert.Equal(4, stats.Total);
            Assert.Equal(1, stats.NotStarted);
            Assert.Equal(1, stats.InProgress);
            Assert.Equal(2, stats.Completed);
        }
    }
}
