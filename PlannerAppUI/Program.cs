using System;
using PlannerApplication.Models;
using PlannerApplication.Repositories;
using PlannerApplication.Services;
using PlannerApplication.Utility;
using PlannerApplication.View;

namespace PlannerAppUI
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Initialize console layout and UTF-8 encoding for crisp box-drawing borders
            ConsoleUI.InitializeConsole();

            // Setup repository and service dependencies
            UserRepository userRepository = new UserRepository();
            ProjectRepository projectRepository = new ProjectRepository();
            TaskRepository taskRepository = new TaskRepository();

            // Seed initial sample data if the repository is empty so the app is instantly usable
            DatabaseSeeder.SeedIfEmpty(userRepository, projectRepository, taskRepository);

            AuthService authService = new AuthService(userRepository);
            ProjectService projectService = new ProjectService(projectRepository, userRepository, taskRepository);
            TaskService taskService = new TaskService(taskRepository);

            AuthMenu authMenu = new AuthMenu(authService);
            ProjectMenu projectMenu = new ProjectMenu(projectService, taskService);

            // Main application loop: Auth -> Projects -> Kanban Board -> Logout -> Auth
            bool appRunning = true;
            while (appRunning)
            {
                User? loggedInUser = authMenu.DisplayAuthPage();
                if (loggedInUser == null)
                {
                    // User chose Exit from the Auth page
                    appRunning = false;
                    break;
                }

                // Launch Project Management View for the authenticated user
                projectMenu.DisplayProjectPage(loggedInUser);
                // When projectMenu returns, the user chose to log out, returning to the Auth page
            }

            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n  Thank you for using PlannerApp! Goodbye.\n");
            Console.ResetColor();
        }
    }
}
