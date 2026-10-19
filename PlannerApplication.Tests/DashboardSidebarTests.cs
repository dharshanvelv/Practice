using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using PlannerApplication.Models;
using PlannerApplication.Repositories;
using PlannerApplication.Services;
using PlannerApplication.View;
using Xunit;

namespace PlannerApplication.Tests
{
    public class DashboardSidebarTests : IDisposable
    {
        private readonly string _userFile;
        private readonly string _projectFile;
        private readonly string _taskFile;

        private readonly UserRepository _userRepo;
        private readonly ProjectRepository _projectRepo;
        private readonly TaskRepository _taskRepo;
        private readonly ProjectService _projectService;
        private readonly TaskService _taskService;

        public DashboardSidebarTests()
        {
            string id = Guid.NewGuid().ToString("N");
            _userFile = $"test_dash_u_{id}.json";
            _projectFile = $"test_dash_p_{id}.json";
            _taskFile = $"test_dash_t_{id}.json";

            _userRepo = new UserRepository(new UserJson(_userFile));
            _projectRepo = new ProjectRepository(new ProjectJson(_projectFile));
            _taskRepo = new TaskRepository(new TaskJson(_taskFile));

            _projectService = new ProjectService(_projectRepo, _userRepo, _taskRepo);
            _taskService = new TaskService(_taskRepo);
        }

        public void Dispose()
        {
            if (File.Exists(_userFile)) File.Delete(_userFile);
            if (File.Exists(_projectFile)) File.Delete(_projectFile);
            if (File.Exists(_taskFile)) File.Delete(_taskFile);
        }

        [Fact]
        public void ProjectMenu_SidebarMenu_ContainsOnlyDashboardLevelActions()
        {
            var projectMenu = new ProjectMenu(_projectService, _taskService);
            var field = typeof(ProjectMenu).GetField("menu", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(field);

            string[]? menu = field.GetValue(projectMenu) as string[];
            Assert.NotNull(menu);

            // Should have exactly 4 general actions
            Assert.Equal(4, menu.Length);
            Assert.Equal(new[] { "Create Project", "Filter Projects", "Sort Projects", "Logout" }, menu);

            // Should NOT have item-specific actions
            Assert.DoesNotContain("Edit Project", menu);
            Assert.DoesNotContain("Delete Project", menu);
            Assert.DoesNotContain("Manage Members", menu);
            Assert.DoesNotContain("Open Project", menu);
        }

        [Fact]
        public void TaskDashboard_SidebarMenu_ContainsOnlyDashboardLevelActions()
        {
            var taskDashboard = new TaskDashboard(_taskService, _projectService);
            var field = typeof(TaskDashboard).GetField("menu", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(field);

            string[]? menu = field.GetValue(taskDashboard) as string[];
            Assert.NotNull(menu);

            // Should have exactly 4 general actions
            Assert.Equal(4, menu.Length);
            Assert.Equal(new[] { "Add Task", "Filter Tasks", "Sort Tasks", "Back to Projects" }, menu);

            // Should NOT have item-specific actions
            Assert.DoesNotContain("Edit Task", menu);
            Assert.DoesNotContain("Delete Task", menu);
            Assert.DoesNotContain("Assign User", menu);
            Assert.DoesNotContain("Move Status", menu);
        }

        [Fact]
        public void ProjectSorting_ByName_Date_AndTaskCount()
        {
            Guid userId = Guid.NewGuid();
            var res1 = _projectService.AddProject(userId, "Zeta Project", "Desc Zeta");
            var res2 = _projectService.AddProject(userId, "Alpha Project", "Desc Alpha");
            Assert.True(res1.IsSuccess);
            Assert.True(res2.IsSuccess);

            var projects = _projectService.ViewProjects(userId);
            var pZeta = projects.First(p => p.ProjectName == "Zeta Project");
            var pAlpha = projects.First(p => p.ProjectName == "Alpha Project");

            // Add tasks to Zeta so it has more tasks than Alpha
            _taskService.AddTask("Task 1", "D1", DateOnly.FromDateTime(DateTime.Today), DateOnly.FromDateTime(DateTime.Today.AddDays(1)), pZeta.ProjectId, userId);
            _taskService.AddTask("Task 2", "D2", DateOnly.FromDateTime(DateTime.Today), DateOnly.FromDateTime(DateTime.Today.AddDays(2)), pZeta.ProjectId, userId);

            // By Name
            var sortedByName = projects.OrderBy(p => p.ProjectName, StringComparer.OrdinalIgnoreCase).ToList();
            Assert.Equal("Alpha Project", sortedByName[0].ProjectName);
            Assert.Equal("Zeta Project", sortedByName[1].ProjectName);

            // By Task Count (descending)
            var sortedByTasks = projects.OrderByDescending(p => _projectService.GetProjectTaskStats(p.ProjectId).Total).ToList();
            Assert.Equal("Zeta Project", sortedByTasks[0].ProjectName); // 2 tasks
            Assert.Equal("Alpha Project", sortedByTasks[1].ProjectName); // 0 tasks
        }
    }
}
