using System;
using System.IO;
using System.Linq;
using PlannerApplication.Models;
using PlannerApplication.Repositories;
using PlannerApplication.Services;
using Xunit;

namespace PlannerApplication.Tests
{
    public class TaskServiceTests : IDisposable
    {
        private readonly string _taskFile;
        private readonly TaskRepository _taskRepo;
        private readonly TaskService _taskService;
        private readonly Guid _projectId;

        public TaskServiceTests()
        {
            _taskFile = $"test_tasks_{Guid.NewGuid():N}.json";
            _taskRepo = new TaskRepository(new TaskJson(_taskFile));
            _taskService = new TaskService(_taskRepo);
            _projectId = Guid.NewGuid();
        }

        public void Dispose()
        {
            if (File.Exists(_taskFile))
            {
                File.Delete(_taskFile);
            }
        }

        [Fact]
        public void AddTask_ValidInput_AddsTaskSuccessfully()
        {
            DateOnly start = DateOnly.FromDateTime(DateTime.Today);
            DateOnly end = start.AddDays(7);

            var result = _taskService.AddTask("Write Documentation", "Detailed API doc", start, end, _projectId);
            Assert.True(result.IsSuccess, $"Failed with message: '{result.ErrorMessage}'");

            var tasks = _taskService.ViewTasks(_projectId);
            Assert.Single(tasks);
            Assert.Equal("Write Documentation", tasks[0].Name);
            Assert.Equal(TaskState.NotStarted, tasks[0].Status);
        }

        [Fact]
        public void AddTask_EmptyName_ReturnsFailure()
        {
            DateOnly today = DateOnly.FromDateTime(DateTime.Today);
            var result = _taskService.AddTask("   ", "Desc", today, today, _projectId);

            Assert.False(result.IsSuccess);
            Assert.Equal("Task name cannot be empty", result.ErrorMessage);
        }

        [Fact]
        public void AddTask_EndDateEarlierThanStartDate_ReturnsFailure()
        {
            DateOnly start = DateOnly.FromDateTime(DateTime.Today);
            DateOnly end = start.AddDays(-2);

            var result = _taskService.AddTask("Time Travel Task", "Desc", start, end, _projectId);
            Assert.False(result.IsSuccess);
            Assert.Contains("earlier", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void EditTask_UpdatesDetailsCorrectly()
        {
            DateOnly start = DateOnly.FromDateTime(DateTime.Today);
            DateOnly end = start.AddDays(5);
            _taskService.AddTask("Initial Task", "Initial Desc", start, end, _projectId);

            var task = _taskService.ViewTasks(_projectId).First();
            DateOnly newEnd = start.AddDays(10);

            var result = _taskService.EditTask(task.Id, "Updated Task", "Updated Desc", start, newEnd, _projectId);
            Assert.True(result.IsSuccess);

            var updated = _taskService.GetTaskById(task.Id);
            Assert.NotNull(updated);
            Assert.Equal("Updated Task", updated.Name);
            Assert.Equal("Updated Desc", updated.Description);
            Assert.Equal(newEnd, updated.TargetEndDate);
        }

        [Fact]
        public void EditTask_PreservesUnchangedFields()
        {
            DateOnly start = DateOnly.FromDateTime(DateTime.Today);
            DateOnly end = start.AddDays(5);
            Guid userA = Guid.NewGuid();
            _taskService.AddTask("Base Task", "Base Description", start, end, _projectId, userA);

            var task = _taskService.ViewTasks(_projectId).First();

            // Edit only Description, preserving name, dates, and user
            var r1 = _taskService.EditTask(task.Id, task.Name, "New Description Only", task.TargetStartDate, task.TargetEndDate, _projectId, task.UserId);
            Assert.True(r1.IsSuccess);

            var u1 = _taskService.GetTaskById(task.Id)!;
            Assert.Equal("Base Task", u1.Name);
            Assert.Equal("New Description Only", u1.Description);
            Assert.Equal(start, u1.TargetStartDate);
            Assert.Equal(end, u1.TargetEndDate);
            Assert.Equal(userA, u1.UserId);

            // Edit only Assignee, preserving others
            Guid userB = Guid.NewGuid();
            var r2 = _taskService.EditTask(task.Id, u1.Name, u1.Description, u1.TargetStartDate, u1.TargetEndDate, _projectId, userB);
            Assert.True(r2.IsSuccess);

            var u2 = _taskService.GetTaskById(task.Id)!;
            Assert.Equal("Base Task", u2.Name);
            Assert.Equal(userB, u2.UserId);
        }

        [Fact]
        public void EditTask_EmptyName_ReturnsFailure()
        {
            DateOnly today = DateOnly.FromDateTime(DateTime.Today);
            _taskService.AddTask("Task Valid", "Desc", today, today, _projectId);
            var task = _taskService.ViewTasks(_projectId).First();

            var result = _taskService.EditTask(task.Id, "   ", "Desc", today, today, _projectId);
            Assert.False(result.IsSuccess);
            Assert.Equal("Task name cannot be empty", result.ErrorMessage);
        }

        [Fact]
        public void EditTask_EndDateBeforeStartDate_ReturnsFailure()
        {
            DateOnly today = DateOnly.FromDateTime(DateTime.Today);
            _taskService.AddTask("Task Valid", "Desc", today, today, _projectId);
            var task = _taskService.ViewTasks(_projectId).First();

            var result = _taskService.EditTask(task.Id, "Task Valid", "Desc", today, today.AddDays(-1), _projectId);
            Assert.False(result.IsSuccess);
            Assert.Contains("earlier", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void UpdateTaskStatus_TransitionsAcrossKanbanStates()
        {
            DateOnly today = DateOnly.FromDateTime(DateTime.Today);
            _taskService.AddTask("Kanban Task", "Desc", today, today, _projectId);
            var task = _taskService.ViewTasks(_projectId).First();

            // Transition: NotStarted -> InProgress
            var r1 = _taskService.UpdateTaskStatus(task.Id, TaskState.InProgress, _projectId);
            Assert.True(r1.IsSuccess);
            Assert.Equal(TaskState.InProgress, _taskService.GetTaskById(task.Id)!.Status);

            // Transition: InProgress -> Completed
            var r2 = _taskService.UpdateTaskStatus(task.Id, TaskState.Completed, _projectId);
            Assert.True(r2.IsSuccess);
            Assert.Equal(TaskState.Completed, _taskService.GetTaskById(task.Id)!.Status);
        }

        [Fact]
        public void DeleteTask_RemovesTask()
        {
            DateOnly today = DateOnly.FromDateTime(DateTime.Today);
            _taskService.AddTask("To Delete", "", today, today, _projectId);
            var task = _taskService.ViewTasks(_projectId).First();

            var result = _taskService.DeleteTask(task.Id, _projectId);
            Assert.True(result.IsSuccess);
            Assert.Empty(_taskService.ViewTasks(_projectId));
        }

        [Fact]
        public void AssignUser_UpdatesAssignedUserId()
        {
            DateOnly today = DateOnly.FromDateTime(DateTime.Today);
            _taskService.AddTask("Task for Alice", "", today, today, _projectId);
            var task = _taskService.ViewTasks(_projectId).First();

            Guid aliceId = Guid.NewGuid();
            var result = _taskService.AssignUser(task.Id, aliceId);
            Assert.True(result.IsSuccess);

            var updated = _taskService.GetTaskById(task.Id);
            Assert.NotNull(updated);
            Assert.Equal(aliceId, updated.UserId);
        }

        [Fact]
        public void FilterTasks_MatchesBothNameAndDescription()
        {
            DateOnly today = DateOnly.FromDateTime(DateTime.Today);
            _taskService.AddTask("Design Database", "PostgreSQL schema", today, today, _projectId);
            _taskService.AddTask("Front-end UI", "React / Console", today, today, _projectId);
            _taskService.AddTask("Backend API", "ASP.NET Core controllers", today, today, _projectId);

            var matchesByName = _taskService.FilterTasks(_projectId, "Database");
            Assert.Single(matchesByName);
            Assert.Equal("Design Database", matchesByName[0].Name);

            var matchesByDesc = _taskService.FilterTasks(_projectId, "controllers");
            Assert.Single(matchesByDesc);
            Assert.Equal("Backend API", matchesByDesc[0].Name);
        }

        [Fact]
        public void SortTasks_OrdersProperly()
        {
            DateOnly today = DateOnly.FromDateTime(DateTime.Today);
            _taskService.AddTask("Charlie Task", "C", today.AddDays(10), today.AddDays(20), _projectId);
            _taskService.AddTask("Alpha Task", "A", today.AddDays(1), today.AddDays(30), _projectId);
            _taskService.AddTask("Bravo Task", "B", today.AddDays(5), today.AddDays(15), _projectId);

            var sortedByStart = _taskService.SortTasks(_projectId, "StartDate");
            Assert.Equal("Alpha Task", sortedByStart[0].Name);
            Assert.Equal("Bravo Task", sortedByStart[1].Name);
            Assert.Equal("Charlie Task", sortedByStart[2].Name);

            var sortedByName = _taskService.SortTasks(_projectId, "Name");
            Assert.Equal("Alpha Task", sortedByName[0].Name);
            Assert.Equal("Bravo Task", sortedByName[1].Name);
            Assert.Equal("Charlie Task", sortedByName[2].Name);

            var sortedByEnd = _taskService.SortTasks(_projectId, "EndDate");
            Assert.Equal("Bravo Task", sortedByEnd[0].Name);
            Assert.Equal("Charlie Task", sortedByEnd[1].Name);
            Assert.Equal("Alpha Task", sortedByEnd[2].Name);
        }
    }
}
