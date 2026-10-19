using PlannerApplication.Models;
using PlannerApplication.Repositories;

namespace PlannerApplication.Utility
{
    public static class DatabaseSeeder
    {
        public static void SeedIfEmpty(UserRepository userRepo, ProjectRepository projectRepo, TaskRepository taskRepo)
        {
            try
            {
                var users = userRepo.GetUsers();
                if (users.Count == 0)
                {
                    User adminUser = new User("admin", SecurityHelper.HashSecret("password123"), SecurityHelper.HashSecret("123456"));
                    User devUser = new User("developer", SecurityHelper.HashSecret("password123"), SecurityHelper.HashSecret("654321"));
                    userRepo.AddUser(adminUser);
                    userRepo.AddUser(devUser);
                    users = userRepo.GetUsers();

                    var projects = projectRepo.GetProjects();
                    if (projects.Count == 0)
                    {
                        DateOnly today = DateOnly.FromDateTime(DateTime.Today);

                        Project project1 = new Project
                        {
                            ProjectId = Guid.NewGuid(),
                            ProjectName = "Website Redesign",
                            ProjectDescription = "Modernize customer-facing portal with responsive design.",
                            CreatedDate = today.AddDays(-14),
                            UserIds = new List<Guid> { adminUser.Id, devUser.Id }
                        };

                        Project project2 = new Project
                        {
                            ProjectId = Guid.NewGuid(),
                            ProjectName = "Mobile App MVP",
                            ProjectDescription = "Cross-platform mobile application with offline task sync.",
                            CreatedDate = today.AddDays(-7),
                            UserIds = new List<Guid> { adminUser.Id }
                        };

                        projectRepo.AddProject(project1);
                        projectRepo.AddProject(project2);

                        // Tasks for Project 1
                        taskRepo.AddTask(new ToDoTask(
                            "User Research & Personas",
                            "Interview 5 key stakeholders and outline target user personas.",
                            today.AddDays(-12),
                            today.AddDays(-5),
                            project1.ProjectId,
                            adminUser.Id)
                        {
                            Status = TaskState.Completed
                        });

                        taskRepo.AddTask(new ToDoTask(
                            "Design System & UI Components",
                            "Craft reusable console/web tokens and card styling specifications.",
                            today.AddDays(-4),
                            today.AddDays(3),
                            project1.ProjectId,
                            devUser.Id)
                        {
                            Status = TaskState.InProgress
                        });

                        taskRepo.AddTask(new ToDoTask(
                            "Implement Responsive Layout",
                            "Build the main grid containers with dynamic terminal resize safety.",
                            today.AddDays(4),
                            today.AddDays(12),
                            project1.ProjectId,
                            devUser.Id)
                        {
                            Status = TaskState.NotStarted
                        });

                        taskRepo.AddTask(new ToDoTask(
                            "End-to-End QA Testing",
                            "Run full user journeys and verify edge-case error recovery.",
                            today.AddDays(13),
                            today.AddDays(20),
                            project1.ProjectId,
                            adminUser.Id)
                        {
                            Status = TaskState.NotStarted
                        });

                        // Tasks for Project 2
                        taskRepo.AddTask(new ToDoTask(
                            "Setup Git Repository & CI",
                            "Configure continuous integration and build pipelines.",
                            today.AddDays(-6),
                            today.AddDays(-2),
                            project2.ProjectId,
                            adminUser.Id)
                        {
                            Status = TaskState.Completed
                        });

                        taskRepo.AddTask(new ToDoTask(
                            "Auth & Token Management",
                            "Implement secure authentication storage and session recovery.",
                            today.AddDays(-1),
                            today.AddDays(5),
                            project2.ProjectId,
                            adminUser.Id)
                        {
                            Status = TaskState.InProgress
                        });
                    }
                }
            }
            catch
            {
                // Silently ignore seeding errors
            }
        }
    }
}
