using System;
using System.Collections.Generic;
using System.Linq;
using PlannerApplication.Models;
using PlannerApplication.Services;
using PlannerApplication.Utility;

namespace PlannerApplication.View
{
    public class ProjectMenu
    {
        private readonly ProjectService _projectService;
        private readonly TaskService _taskService;
        private readonly TaskDashboard _taskDashboard;

        private readonly string[] menu = { "Open Project", "Create Project", "Edit Project", "Delete Project", "Manage Members", "Logout" };

        private int SearchBoxCursorX => ConsoleUI.WindowWidth - 22;
        private int SearchBoxCursorY => 3;
        private int SidebarWidth => Math.Max(18, ConsoleUI.WindowWidth / 5);

        private enum FocusArea
        {
            Sidebar,
            ProjectList,
            SearchBox
        }

        public ProjectMenu(ProjectService? projectService = null, TaskService? taskService = null)
        {
            _projectService = projectService ?? new ProjectService();
            _taskService = taskService ?? new TaskService();
            _taskDashboard = new TaskDashboard(_taskService, _projectService);
        }

        public void DisplayProjectPage(User? currentUser = null)
        {
            Guid currentUserId = currentUser?.Id ?? Guid.Empty;
            string currentUserName = currentUser?.Name ?? "Guest";

            FocusArea currentFocus = FocusArea.ProjectList;
            int sidebarIndex = 0;
            int projectIndex = 0;
            string searchFilter = string.Empty;
            string statusMessage = string.Empty;
            bool isStatusError = false;

            bool isPageRunning = true;
            while (isPageRunning)
            {
                var allProjects = _projectService.ViewProjects(currentUserId);
                var displayProjects = string.IsNullOrWhiteSpace(searchFilter)
                    ? allProjects
                    : allProjects.Where(p =>
                        p.ProjectName.Contains(searchFilter, StringComparison.OrdinalIgnoreCase) ||
                        p.ProjectDescription.Contains(searchFilter, StringComparison.OrdinalIgnoreCase)
                    ).ToList();

                if (displayProjects.Count == 0 && currentFocus == FocusArea.ProjectList)
                {
                    currentFocus = FocusArea.Sidebar;
                }

                if (projectIndex >= displayProjects.Count)
                {
                    projectIndex = Math.Max(0, displayProjects.Count - 1);
                }

                DrawProjectMenuLayout(currentUserName, searchFilter);
                DrawSidebarMenu(sidebarIndex, currentFocus == FocusArea.Sidebar);
                DrawContentMenu(displayProjects, projectIndex, currentFocus == FocusArea.ProjectList);

                // Show status / shortcut help
                string helpText = currentFocus switch
                {
                    FocusArea.Sidebar => "[↑/↓] Select Action  [→] Go to Projects  [Enter] Execute  [Esc] Logout",
                    FocusArea.ProjectList => "[↑/↓] Select Project  [←] Sidebar  [→] Search  [Enter] Open  [C] Create  [D] Delete",
                    FocusArea.SearchBox => "[Enter] Search  [Esc] Clear/Exit Search  [←] Projects",
                    _ => ""
                };
                ConsoleUI.ShowStatusBar(helpText, statusMessage, isStatusError);
                statusMessage = string.Empty;

                // Handle input based on current focus
                if (currentFocus == FocusArea.Sidebar)
                {
                    var key = Console.ReadKey(true).Key;
                    switch (key)
                    {
                        case ConsoleKey.UpArrow:
                            if (sidebarIndex > 0) sidebarIndex--;
                            break;

                        case ConsoleKey.DownArrow:
                            if (sidebarIndex < menu.Length - 1) sidebarIndex++;
                            break;

                        case ConsoleKey.RightArrow:
                            if (displayProjects.Count > 0)
                            {
                                currentFocus = FocusArea.ProjectList;
                            }
                            else
                            {
                                currentFocus = FocusArea.SearchBox;
                            }
                            break;

                        case ConsoleKey.Enter:
                            ExecuteSidebarAction(sidebarIndex, displayProjects, projectIndex, currentUser, ref statusMessage, ref isStatusError, ref isPageRunning, ref currentFocus);
                            break;

                        case ConsoleKey.Escape:
                            if (ConsoleUI.ShowConfirmation("Logout", "Are you sure you want to log out?"))
                            {
                                return;
                            }
                            break;
                    }
                }
                else if (currentFocus == FocusArea.ProjectList)
                {
                    var keyInfo = Console.ReadKey(true);
                    switch (keyInfo.Key)
                    {
                        case ConsoleKey.UpArrow:
                            if (projectIndex > 0) projectIndex--;
                            break;

                        case ConsoleKey.DownArrow:
                            if (projectIndex < displayProjects.Count - 1) projectIndex++;
                            break;

                        case ConsoleKey.LeftArrow:
                            currentFocus = FocusArea.Sidebar;
                            break;

                        case ConsoleKey.RightArrow:
                            currentFocus = FocusArea.SearchBox;
                            break;

                        case ConsoleKey.Enter:
                            if (displayProjects.Count > 0 && projectIndex >= 0 && projectIndex < displayProjects.Count)
                            {
                                var selectedProject = displayProjects[projectIndex];
                                _taskDashboard.DisplayDashboard(currentUser, selectedProject);
                            }
                            break;

                        case ConsoleKey.C:
                            CreateProjectModal(currentUser, ref statusMessage, ref isStatusError);
                            break;

                        case ConsoleKey.E:
                            if (displayProjects.Count > 0)
                            {
                                EditProjectModal(displayProjects[projectIndex], currentUser, ref statusMessage, ref isStatusError);
                            }
                            break;

                        case ConsoleKey.D:
                            if (displayProjects.Count > 0)
                            {
                                DeleteProjectModal(displayProjects[projectIndex], currentUser, ref statusMessage, ref isStatusError);
                            }
                            break;

                        case ConsoleKey.M:
                            if (displayProjects.Count > 0)
                            {
                                ManageMembersModal(displayProjects[projectIndex], currentUser, ref statusMessage, ref isStatusError);
                            }
                            break;

                        case ConsoleKey.Escape:
                            if (!string.IsNullOrEmpty(searchFilter))
                            {
                                searchFilter = string.Empty;
                                statusMessage = "Search cleared";
                            }
                            else
                            {
                                currentFocus = FocusArea.Sidebar;
                            }
                            break;
                    }
                }
                else if (currentFocus == FocusArea.SearchBox)
                {
                    HighlightSearchBox(true);
                    ConsoleUI.SafeSetCursorPosition(SearchBoxCursorX, SearchBoxCursorY);
                    string query = ConsoleUI.ReadTextInput(SearchBoxCursorX, SearchBoxCursorY, 18, searchFilter, allowEscape: true);

                    searchFilter = query.Trim();
                    currentFocus = FocusArea.ProjectList;
                    projectIndex = 0;
                }
            }
        }

        private void ExecuteSidebarAction(
            int actionIndex,
            List<Project> displayProjects,
            int projectIndex,
            User? currentUser,
            ref string statusMessage,
            ref bool isStatusError,
            ref bool isPageRunning,
            ref FocusArea currentFocus)
        {
            switch (actionIndex)
            {
                case 0: // Open Project
                    if (displayProjects.Count > 0 && projectIndex >= 0 && projectIndex < displayProjects.Count)
                    {
                        var selectedProject = displayProjects[projectIndex];
                        _taskDashboard.DisplayDashboard(currentUser, selectedProject);
                    }
                    else
                    {
                        statusMessage = "No project selected to open";
                        isStatusError = true;
                        currentFocus = FocusArea.ProjectList;
                    }
                    break;

                case 1: // Create Project
                    CreateProjectModal(currentUser, ref statusMessage, ref isStatusError);
                    break;

                case 2: // Edit Project
                    if (displayProjects.Count > 0 && projectIndex >= 0 && projectIndex < displayProjects.Count)
                    {
                        EditProjectModal(displayProjects[projectIndex], currentUser, ref statusMessage, ref isStatusError);
                    }
                    else
                    {
                        statusMessage = "Select a project first to edit";
                        isStatusError = true;
                    }
                    break;

                case 3: // Delete Project
                    if (displayProjects.Count > 0 && projectIndex >= 0 && projectIndex < displayProjects.Count)
                    {
                        DeleteProjectModal(displayProjects[projectIndex], currentUser, ref statusMessage, ref isStatusError);
                    }
                    else
                    {
                        statusMessage = "Select a project first to delete";
                        isStatusError = true;
                    }
                    break;

                case 4: // Manage Members
                    if (displayProjects.Count > 0 && projectIndex >= 0 && projectIndex < displayProjects.Count)
                    {
                        ManageMembersModal(displayProjects[projectIndex], currentUser, ref statusMessage, ref isStatusError);
                    }
                    else
                    {
                        statusMessage = "Select a project first to manage members";
                        isStatusError = true;
                    }
                    break;

                case 5: // Logout
                    if (ConsoleUI.ShowConfirmation("Logout", "Are you sure you want to log out?"))
                    {
                        isPageRunning = false;
                    }
                    break;
            }
        }

        public void DrawProjectMenuLayout(string username = "", string searchFilter = "")
        {
            Console.Clear();
            DrawBoundary();
            DrawSidebarLine();
            DrawHeader(username);
            DrawSearchBox(searchFilter);
        }

        private void DrawHeader(string username)
        {
            string appTitle = "PLANNER APP  |  Projects";
            ConsoleUI.WriteAt(3, 1, $" {appTitle} ", ConsoleColor.Cyan);

            if (!string.IsNullOrEmpty(username))
            {
                string userTag = $" \uD83D\uDC64 User: {username} ";
                int userX = SidebarWidth + 3;
                ConsoleUI.WriteAt(userX, 1, userTag, ConsoleColor.Yellow);
            }
        }

        public void DrawSidebarMenu(int select, bool hasFocus = true)
        {
            Console.CursorVisible = false;
            ConsoleUI.WriteAt(3, 3, "ACTIONS", ConsoleColor.DarkGray);

            for (int i = 0; i < menu.Length; i++)
            {
                int y = 5 + (i * 2);
                string clearLine = new string(' ', SidebarWidth - 4);
                ConsoleUI.WriteAt(3, y, clearLine);

                if (i == select && hasFocus)
                {
                    ConsoleUI.WriteAt(3, y, $"\u25BA {menu[i]}", ConsoleColor.Blue);
                }
                else if (i == select)
                {
                    ConsoleUI.WriteAt(3, y, $"  {menu[i]}", ConsoleColor.DarkCyan);
                }
                else
                {
                    ConsoleUI.WriteAt(3, y, $"  {menu[i]}", ConsoleColor.Gray);
                }
            }
        }

        public void DrawContentMenu(List<Project> projects, int select, bool hasFocus = true)
        {
            Console.CursorVisible = false;
            int startX = SidebarWidth + 3;
            int startY = 5;
            int availableWidth = ConsoleUI.WindowWidth - startX - 3;
            int maxRows = ConsoleUI.WindowHeight - 10;

            // Clear content area
            ConsoleUI.ClearArea(startX, startY, availableWidth, maxRows);

            if (projects == null || projects.Count == 0)
            {
                ConsoleUI.WriteAt(startX + 4, startY + 2, "No projects found.", ConsoleColor.DarkGray);
                ConsoleUI.WriteAt(startX + 4, startY + 4, "Press [C] or choose 'Create Project' in the sidebar to add one.", ConsoleColor.DarkCyan);
                return;
            }

            // Draw table header
            int col1 = startX;
            int col2 = startX + 28;
            int col3 = startX + 48;
            int col4 = startX + 66;

            ConsoleUI.WriteAt(col1, startY, "PROJECT NAME", ConsoleColor.DarkGray);
            if (col2 < ConsoleUI.WindowWidth - 15) ConsoleUI.WriteAt(col2, startY, "TASKS", ConsoleColor.DarkGray);
            if (col3 < ConsoleUI.WindowWidth - 15) ConsoleUI.WriteAt(col3, startY, "MEMBERS", ConsoleColor.DarkGray);
            if (col4 < ConsoleUI.WindowWidth - 15) ConsoleUI.WriteAt(col4, startY, "CREATED", ConsoleColor.DarkGray);

            string divLine = new string(ConsoleUI.Horizontal, Math.Max(0, availableWidth));
            ConsoleUI.WriteAt(startX, startY + 1, divLine, ConsoleColor.DarkGray);

            int displayCount = Math.Min(projects.Count, (maxRows - 3) / 2);

            for (int i = 0; i < displayCount; i++)
            {
                var proj = projects[i];
                int rowY = startY + 2 + (i * 2);
                bool isSelected = (i == select);

                var stats = _projectService.GetProjectTaskStats(proj.ProjectId);
                string taskSummary = $"{stats.Total} tasks ({stats.Completed} done)";
                string memberCount = $"{proj.UserIds?.Count ?? 0} members";
                string dateStr = proj.CreatedDate.ToString("yyyy-MM-dd");

                ConsoleColor itemColor = (isSelected && hasFocus) ? ConsoleColor.Blue : (isSelected ? ConsoleColor.DarkCyan : ConsoleColor.White);
                string prefix = (isSelected && hasFocus) ? "\u25BA " : "  ";

                string namePadded = ConsoleUI.Truncate(prefix + proj.ProjectName, 26);
                ConsoleUI.WriteAt(col1, rowY, namePadded, itemColor);

                if (col2 < ConsoleUI.WindowWidth - 15)
                {
                    ConsoleUI.WriteAt(col2, rowY, ConsoleUI.Truncate(taskSummary, 18), ConsoleColor.Gray);
                }

                if (col3 < ConsoleUI.WindowWidth - 15)
                {
                    ConsoleUI.WriteAt(col3, rowY, ConsoleUI.Truncate(memberCount, 16), ConsoleColor.DarkGray);
                }

                if (col4 < ConsoleUI.WindowWidth - 15)
                {
                    ConsoleUI.WriteAt(col4, rowY, dateStr, ConsoleColor.DarkGray);
                }
            }

            if (projects.Count > displayCount)
            {
                ConsoleUI.WriteAt(startX, startY + 2 + (displayCount * 2), $"... and {projects.Count - displayCount} more projects", ConsoleColor.DarkGray);
            }
        }

        public void DrawBoundary()
        {
            ConsoleUI.DrawBoundary();
        }

        public void DrawSidebarLine()
        {
            for (int i = 2; i < ConsoleUI.WindowHeight - 1; i++)
            {
                ConsoleUI.SafeSetCursorPosition(SidebarWidth, i);
                Console.Write(ConsoleUI.Vertical);
            }
        }

        public void DrawSearchBox(string currentFilter = "")
        {
            int labelX = ConsoleUI.WindowWidth - 29;
            int boxX = ConsoleUI.WindowWidth - 23;

            ConsoleUI.WriteAt(labelX, 3, "Search", ConsoleColor.Gray);
            ConsoleUI.DrawFieldBox(boxX, 2, 20, ConsoleColor.Gray);

            if (!string.IsNullOrEmpty(currentFilter))
            {
                ConsoleUI.WriteAt(boxX + 1, 3, ConsoleUI.Truncate(currentFilter, 18), ConsoleColor.Cyan);
            }
        }

        private void HighlightSearchBox(bool active)
        {
            int labelX = ConsoleUI.WindowWidth - 29;
            int boxX = ConsoleUI.WindowWidth - 23;
            ConsoleColor color = active ? ConsoleColor.Blue : ConsoleColor.Gray;

            ConsoleUI.WriteAt(labelX, 3, "Search", color);
            ConsoleUI.DrawFieldBox(boxX, 2, 20, color);
        }

        public void DrawFieldBox(int length)
        {
            var curPosition = Console.GetCursorPosition();
            ConsoleUI.DrawFieldBox(curPosition.Left, curPosition.Top, length);
        }

        // ================= Modals =================

        private void CreateProjectModal(User? currentUser, ref string statusMessage, ref bool isStatusError)
        {
            var (startX, startY, width, height) = ConsoleUI.DrawModalDialog(64, 15, "CREATE NEW PROJECT");

            ConsoleUI.WriteAt(startX + 4, startY + 4, "Project Name : ", ConsoleColor.White);
            ConsoleUI.DrawFieldBox(startX + 20, startY + 3, 38, ConsoleColor.White);

            ConsoleUI.WriteAt(startX + 4, startY + 7, "Description  : ", ConsoleColor.White);
            ConsoleUI.DrawFieldBox(startX + 20, startY + 6, 38, ConsoleColor.White);

            ConsoleUI.WriteAt(startX + 4, startY + 10, "Members      : ", ConsoleColor.White);
            ConsoleUI.DrawFieldBox(startX + 20, startY + 9, 38, ConsoleColor.White);
            ConsoleUI.WriteAt(startX + 20, startY + 12, "(comma-separated usernames, optional)", ConsoleColor.DarkGray);

            ConsoleUI.WriteAt(startX + 4, startY + 13, "[Enter] Next/Submit   [Esc] Cancel", ConsoleColor.DarkCyan);

            // Read Name
            string name = ConsoleUI.ReadTextInput(startX + 21, startY + 4, 36);
            if (string.IsNullOrWhiteSpace(name))
            {
                statusMessage = "Project creation cancelled";
                isStatusError = false;
                return;
            }

            // Read Description
            string desc = ConsoleUI.ReadTextInput(startX + 21, startY + 7, 36);

            // Read Members
            string membersRaw = ConsoleUI.ReadTextInput(startX + 21, startY + 10, 36);
            string[] memberArray = string.IsNullOrWhiteSpace(membersRaw)
                ? Array.Empty<string>()
                : membersRaw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var result = _projectService.AddProject(currentUser?.Id ?? Guid.Empty, name, desc, memberArray);
            if (result.IsSuccess)
            {
                statusMessage = $"Project '{name.Trim()}' created successfully!";
                isStatusError = false;
            }
            else
            {
                statusMessage = result.ErrorMessage;
                isStatusError = true;
            }
        }

        private void EditProjectModal(Project project, User? currentUser, ref string statusMessage, ref bool isStatusError)
        {
            string editedName = project.ProjectName;
            string editedDesc = project.ProjectDescription;

            int selectedOption = 0;
            const int optionCount = 3; // 0: Project Name, 1: Description, 2: Save Changes
            string errorMessage = string.Empty;

            const int modalWidth = 66;
            const int modalHeight = 16;
            const int fieldLength = 36;

            while (true)
            {
                var (startX, startY, width, height) = ConsoleUI.DrawModalDialog(modalWidth, modalHeight, $"EDIT PROJECT: {ConsoleUI.Truncate(project.ProjectName, 20)}");

                ConsoleUI.WriteAt(startX + 4, startY + 3, "Select a field to edit, then select [Save Changes]:", ConsoleColor.DarkGray);

                // Option 0: Project Name
                int opt0Y = startY + 5;
                bool isOpt0 = (selectedOption == 0);
                string prefix0 = isOpt0 ? "\u25BA " : "  ";
                ConsoleColor labelCol0 = isOpt0 ? ConsoleColor.Cyan : ConsoleColor.White;
                ConsoleColor boxCol0 = isOpt0 ? ConsoleColor.Blue : ConsoleColor.DarkGray;
                ConsoleColor textCol0 = isOpt0 ? ConsoleColor.Yellow : ConsoleColor.White;

                ConsoleUI.WriteAt(startX + 3, opt0Y, prefix0 + "Project Name : ", labelCol0);
                ConsoleUI.WriteAt(startX + 18, opt0Y, "[ ", boxCol0);
                string dispName = ConsoleUI.Truncate(editedName, fieldLength).PadRight(fieldLength);
                ConsoleUI.WriteAt(startX + 20, opt0Y, dispName, textCol0);
                ConsoleUI.WriteAt(startX + 20 + fieldLength, opt0Y, " ]", boxCol0);

                // Option 1: Description
                int opt1Y = startY + 7;
                bool isOpt1 = (selectedOption == 1);
                string prefix1 = isOpt1 ? "\u25BA " : "  ";
                ConsoleColor labelCol1 = isOpt1 ? ConsoleColor.Cyan : ConsoleColor.White;
                ConsoleColor boxCol1 = isOpt1 ? ConsoleColor.Blue : ConsoleColor.DarkGray;
                ConsoleColor textCol1 = isOpt1 ? ConsoleColor.Yellow : ConsoleColor.White;

                ConsoleUI.WriteAt(startX + 3, opt1Y, prefix1 + "Description  : ", labelCol1);
                ConsoleUI.WriteAt(startX + 18, opt1Y, "[ ", boxCol1);
                string dispDesc = ConsoleUI.Truncate(editedDesc, fieldLength).PadRight(fieldLength);
                ConsoleUI.WriteAt(startX + 20, opt1Y, dispDesc, textCol1);
                ConsoleUI.WriteAt(startX + 20 + fieldLength, opt1Y, " ]", boxCol1);

                // Option 2: Save Changes
                int opt2Y = startY + 10;
                bool isOpt2 = (selectedOption == 2);
                string prefix2 = isOpt2 ? "\u25BA " : "  ";
                ConsoleColor saveCol = isOpt2 ? ConsoleColor.Blue : ConsoleColor.Green;
                ConsoleUI.WriteAt(startX + 3, opt2Y, $"{prefix2}[ Save Changes ]", saveCol);

                // Validation / Error message line
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    ConsoleUI.WriteAt(startX + 4, startY + 12, $"\u25B2 {errorMessage}", ConsoleColor.Red);
                }

                // Footer shortcuts
                ConsoleUI.WriteAt(startX + 4, startY + 14, "[↑/↓] Navigate   [Enter] Edit Field / Save   [Esc] Cancel", ConsoleColor.DarkCyan);

                var key = Console.ReadKey(true).Key;
                switch (key)
                {
                    case ConsoleKey.UpArrow:
                        if (selectedOption > 0) selectedOption--;
                        else selectedOption = optionCount - 1;
                        errorMessage = string.Empty;
                        break;

                    case ConsoleKey.DownArrow:
                        if (selectedOption < optionCount - 1) selectedOption++;
                        else selectedOption = 0;
                        errorMessage = string.Empty;
                        break;

                    case ConsoleKey.Enter:
                        if (selectedOption == 0) // Edit Project Name
                        {
                            ConsoleUI.WriteAt(startX + 20, opt0Y, new string(' ', fieldLength));
                            ConsoleUI.SafeSetCursorPosition(startX + 20, opt0Y);
                            string input = ConsoleUI.ReadTextInput(startX + 20, opt0Y, fieldLength, editedName, allowEscape: true);
                            editedName = input.Trim();
                            errorMessage = string.Empty;
                        }
                        else if (selectedOption == 1) // Edit Description
                        {
                            ConsoleUI.WriteAt(startX + 20, opt1Y, new string(' ', fieldLength));
                            ConsoleUI.SafeSetCursorPosition(startX + 20, opt1Y);
                            string input = ConsoleUI.ReadTextInput(startX + 20, opt1Y, fieldLength, editedDesc, allowEscape: true);
                            editedDesc = input.Trim();
                            errorMessage = string.Empty;
                        }
                        else if (selectedOption == 2) // Save Changes
                        {
                            if (string.IsNullOrWhiteSpace(editedName))
                            {
                                errorMessage = "Project name cannot be empty";
                                break;
                            }

                            var result = _projectService.EditProject(project.ProjectId, currentUser?.Id ?? Guid.Empty, editedName, editedDesc);
                            if (result.IsSuccess)
                            {
                                project.ProjectName = editedName;
                                project.ProjectDescription = editedDesc;
                                statusMessage = $"Project '{editedName}' updated successfully!";
                                isStatusError = false;
                                return;
                            }
                            else
                            {
                                errorMessage = result.ErrorMessage;
                            }
                        }
                        break;

                    case ConsoleKey.Escape:
                        bool changed = (editedName != project.ProjectName || editedDesc != project.ProjectDescription);
                        if (changed)
                        {
                            if (ConsoleUI.ShowConfirmation("Discard Changes", "Discard unsaved changes and exit?"))
                            {
                                return;
                            }
                        }
                        else
                        {
                            return;
                        }
                        break;
                }
            }
        }

        private void DeleteProjectModal(Project project, User? currentUser, ref string statusMessage, ref bool isStatusError)
        {
            bool confirmed = ConsoleUI.ShowConfirmation(
                "DELETE PROJECT",
                $"Delete '{project.ProjectName}' and all associated tasks?"
            );

            if (confirmed)
            {
                var result = _projectService.CloseProject(project.ProjectId, currentUser?.Id ?? Guid.Empty);
                if (result.IsSuccess)
                {
                    statusMessage = $"Project '{project.ProjectName}' deleted.";
                    isStatusError = false;
                }
                else
                {
                    statusMessage = result.ErrorMessage;
                    isStatusError = true;
                }
            }
        }

        private void ManageMembersModal(Project project, User? currentUser, ref string statusMessage, ref bool isStatusError)
        {
            var (startX, startY, width, height) = ConsoleUI.DrawModalDialog(64, 15, $"MEMBERS: {ConsoleUI.Truncate(project.ProjectName, 20)}");

            var allUsers = _projectService.ViewUsers();
            var memberUsers = allUsers.Where(u => project.UserIds != null && project.UserIds.Contains(u.Id)).ToList();

            ConsoleUI.WriteAt(startX + 4, startY + 3, "Current Members:", ConsoleColor.Yellow);
            if (memberUsers.Count == 0)
            {
                ConsoleUI.WriteAt(startX + 6, startY + 5, "(No members assigned - public project)", ConsoleColor.DarkGray);
            }
            else
            {
                string memberList = string.Join(", ", memberUsers.Select(u => u.Name));
                ConsoleUI.WriteAt(startX + 6, startY + 5, ConsoleUI.Truncate(memberList, 52), ConsoleColor.White);
            }

            ConsoleUI.WriteAt(startX + 4, startY + 7, "Add Member(s) : ", ConsoleColor.White);
            ConsoleUI.DrawFieldBox(startX + 20, startY + 6, 38, ConsoleColor.White);
            ConsoleUI.WriteAt(startX + 20, startY + 9, "(Enter usernames separated by commas)", ConsoleColor.DarkGray);

            ConsoleUI.WriteAt(startX + 4, startY + 12, "[Enter] Add   [Esc] Close", ConsoleColor.DarkCyan);

            string toAdd = ConsoleUI.ReadTextInput(startX + 21, startY + 7, 36);
            if (!string.IsNullOrWhiteSpace(toAdd))
            {
                var names = toAdd.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var result = _projectService.AddMembers(project.ProjectId, currentUser?.Id ?? Guid.Empty, names);
                if (result.IsSuccess)
                {
                    statusMessage = $"Members added to '{project.ProjectName}'.";
                    isStatusError = false;
                }
                else
                {
                    statusMessage = result.ErrorMessage;
                    isStatusError = true;
                }
            }
        }
    }
}
