using System;
using System.Collections.Generic;
using System.Linq;
using PlannerApplication.Models;
using PlannerApplication.Services;
using PlannerApplication.Utility;

namespace PlannerApplication.View
{
    public class TaskDashboard
    {
        private readonly TaskService _taskService;
        private readonly ProjectService _projectService;

        private readonly string[] menu =
        {
            "Add Task",
            "Move Status",
            "Edit Task",
            "Delete Task",
            "Assign User",
            "Filter Tasks",
            "Sort Tasks",
            "Back to Projects"
        };

        private enum FocusArea
        {
            Sidebar,
            KanbanBoard
        }

        public TaskDashboard(TaskService? taskService = null, ProjectService? projectService = null)
        {
            _taskService = taskService ?? new TaskService();
            _projectService = projectService ?? new ProjectService();
        }

        public void DisplayDashboard(User? currentUser, Project project)
        {
            FocusArea currentFocus = FocusArea.KanbanBoard;
            int sidebarIndex = 0;
            int selectedColumn = 0; // 0: NotStarted, 1: InProgress, 2: Completed
            int[] columnTaskIndex = new int[3] { 0, 0, 0 };

            string searchFilter = string.Empty;
            string currentSort = "StartDate"; // "StartDate", "EndDate", "Name"
            string statusMessage = string.Empty;
            bool isStatusError = false;

            bool isRunning = true;
            while (isRunning)
            {
                // Fetch tasks for this project
                var allProjectTasks = string.IsNullOrWhiteSpace(searchFilter)
                    ? _taskService.SortTasks(project.ProjectId, currentSort)
                    : _taskService.FilterTasks(project.ProjectId, searchFilter);

                var notStartedTasks = allProjectTasks.Where(t => t.Status == TaskState.NotStarted).ToList();
                var inProgressTasks = allProjectTasks.Where(t => t.Status == TaskState.InProgress).ToList();
                var completedTasks = allProjectTasks.Where(t => t.Status == TaskState.Completed).ToList();

                List<ToDoTask>[] columns = new List<ToDoTask>[]
                {
                    notStartedTasks,
                    inProgressTasks,
                    completedTasks
                };

                // Clamp selections
                for (int c = 0; c < 3; c++)
                {
                    if (columns[c].Count == 0)
                    {
                        columnTaskIndex[c] = 0;
                    }
                    else if (columnTaskIndex[c] >= columns[c].Count)
                    {
                        columnTaskIndex[c] = columns[c].Count - 1;
                    }
                }

                DrawDashboardLayout(project, notStartedTasks.Count, inProgressTasks.Count, completedTasks.Count, searchFilter, currentSort);
                DrawSidebarMenu(sidebarIndex, currentFocus == FocusArea.Sidebar);
                DrawContainers(columns, selectedColumn, columnTaskIndex, currentFocus == FocusArea.KanbanBoard);

                // Show status / shortcut help
                string helpText = currentFocus == FocusArea.Sidebar
                    ? "[↑/↓] Action  [→] Board  [Enter] Select  [Esc] Back"
                    : "[←/→] Column  [↑/↓] Task  [Enter] Details  [M] Move  [+] Add  [E] Edit  [D] Delete  [Esc] Sidebar";

                ConsoleUI.ShowStatusBar(helpText, statusMessage, isStatusError);
                statusMessage = string.Empty;

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
                            currentFocus = FocusArea.KanbanBoard;
                            break;

                        case ConsoleKey.Enter:
                            ExecuteSidebarAction(
                                sidebarIndex,
                                project,
                                currentUser,
                                columns,
                                selectedColumn,
                                columnTaskIndex[selectedColumn],
                                ref searchFilter,
                                ref currentSort,
                                ref statusMessage,
                                ref isStatusError,
                                ref isRunning,
                                ref currentFocus);
                            break;

                        case ConsoleKey.Escape:
                            isRunning = false;
                            break;
                    }
                }
                else // KanbanBoard focus
                {
                    var keyInfo = Console.ReadKey(true);
                    var activeColumnTasks = columns[selectedColumn];
                    int activeTaskIndex = columnTaskIndex[selectedColumn];
                    ToDoTask? selectedTask = (activeColumnTasks.Count > 0 && activeTaskIndex < activeColumnTasks.Count)
                        ? activeColumnTasks[activeTaskIndex]
                        : null;

                    switch (keyInfo.Key)
                    {
                        case ConsoleKey.LeftArrow:
                            if (selectedColumn > 0)
                            {
                                selectedColumn--;
                            }
                            else
                            {
                                currentFocus = FocusArea.Sidebar;
                            }
                            break;

                        case ConsoleKey.RightArrow:
                            if (selectedColumn < 2)
                            {
                                selectedColumn++;
                            }
                            break;

                        case ConsoleKey.UpArrow:
                            if (activeTaskIndex > 0)
                            {
                                columnTaskIndex[selectedColumn]--;
                            }
                            break;

                        case ConsoleKey.DownArrow:
                            if (activeTaskIndex < activeColumnTasks.Count - 1)
                            {
                                columnTaskIndex[selectedColumn]++;
                            }
                            break;

                        case ConsoleKey.Enter:
                            if (selectedTask != null)
                            {
                                TaskDetailsModal(selectedTask, project, currentUser, ref statusMessage, ref isStatusError);
                            }
                            else
                            {
                                AddTaskModal(project, currentUser, (TaskState)selectedColumn, ref statusMessage, ref isStatusError);
                            }
                            break;

                        case ConsoleKey.M: // Move Status
                            if (selectedTask != null)
                            {
                                QuickMoveTaskModal(selectedTask, ref statusMessage, ref isStatusError);
                            }
                            break;

                        case ConsoleKey.OemPlus:
                        case ConsoleKey.Add:
                        case ConsoleKey.A:
                            AddTaskModal(project, currentUser, (TaskState)selectedColumn, ref statusMessage, ref isStatusError);
                            break;

                        case ConsoleKey.E:
                            if (selectedTask != null)
                            {
                                EditTaskModal(selectedTask, project, currentUser, ref statusMessage, ref isStatusError);
                            }
                            break;

                        case ConsoleKey.D:
                        case ConsoleKey.Delete:
                            if (selectedTask != null)
                            {
                                DeleteTaskModal(selectedTask, ref statusMessage, ref isStatusError);
                            }
                            break;

                        case ConsoleKey.U:
                            if (selectedTask != null)
                            {
                                AssignUserModal(selectedTask, project, ref statusMessage, ref isStatusError);
                            }
                            break;

                        case ConsoleKey.F:
                        case ConsoleKey.Oem2: // '/'
                            FilterTasksModal(ref searchFilter, ref statusMessage);
                            break;

                        case ConsoleKey.S:
                            currentSort = currentSort switch
                            {
                                "StartDate" => "EndDate",
                                "EndDate" => "Name",
                                "Name" => "Status",
                                _ => "StartDate"
                            };
                            statusMessage = $"Tasks sorted by: {currentSort}";
                            break;

                        case ConsoleKey.Escape:
                            if (!string.IsNullOrEmpty(searchFilter))
                            {
                                searchFilter = string.Empty;
                                statusMessage = "Filter cleared";
                            }
                            else
                            {
                                currentFocus = FocusArea.Sidebar;
                            }
                            break;
                    }
                }
            }
        }

        private void ExecuteSidebarAction(
            int actionIndex,
            Project project,
            User? currentUser,
            List<ToDoTask>[] columns,
            int selectedColumn,
            int selectedTaskIndex,
            ref string searchFilter,
            ref string currentSort,
            ref string statusMessage,
            ref bool isStatusError,
            ref bool isRunning,
            ref FocusArea currentFocus)
        {
            var activeColumnTasks = columns[selectedColumn];
            ToDoTask? selectedTask = (activeColumnTasks.Count > 0 && selectedTaskIndex < activeColumnTasks.Count)
                ? activeColumnTasks[selectedTaskIndex]
                : null;

            switch (actionIndex)
            {
                case 0: // Add Task
                    AddTaskModal(project, currentUser, (TaskState)selectedColumn, ref statusMessage, ref isStatusError);
                    break;

                case 1: // Move Status
                    if (selectedTask != null)
                    {
                        QuickMoveTaskModal(selectedTask, ref statusMessage, ref isStatusError);
                    }
                    else
                    {
                        statusMessage = "Select a task first to move its status";
                        isStatusError = true;
                    }
                    break;

                case 2: // Edit Task
                    if (selectedTask != null)
                    {
                        EditTaskModal(selectedTask, project, currentUser, ref statusMessage, ref isStatusError);
                    }
                    else
                    {
                        statusMessage = "Select a task first to edit";
                        isStatusError = true;
                    }
                    break;

                case 3: // Delete Task
                    if (selectedTask != null)
                    {
                        DeleteTaskModal(selectedTask, ref statusMessage, ref isStatusError);
                    }
                    else
                    {
                        statusMessage = "Select a task first to delete";
                        isStatusError = true;
                    }
                    break;

                case 4: // Assign User
                    if (selectedTask != null)
                    {
                        AssignUserModal(selectedTask, project, ref statusMessage, ref isStatusError);
                    }
                    else
                    {
                        statusMessage = "Select a task first to assign a user";
                        isStatusError = true;
                    }
                    break;

                case 5: // Filter Tasks
                    FilterTasksModal(ref searchFilter, ref statusMessage);
                    break;

                case 6: // Sort Tasks
                    currentSort = currentSort switch
                    {
                        "StartDate" => "EndDate",
                        "EndDate" => "Name",
                        "Name" => "Status",
                        _ => "StartDate"
                    };
                    statusMessage = $"Tasks sorted by: {currentSort}";
                    break;

                case 7: // Back to Projects
                    isRunning = false;
                    break;
            }
        }

        // ================= Layout & Drawing =================

        private int SidebarWidth => Math.Max(18, ConsoleUI.WindowWidth / 5);

        private (int colWidth, int col1X, int col2X, int col3X, int colY, int colHeight) GetColumnCoordinates()
        {
            int colY = 6;
            int colHeight = ConsoleUI.WindowHeight - colY - 3;
            int availableWidth = ConsoleUI.WindowWidth - SidebarWidth - 4;
            int colWidth = Math.Max(20, (availableWidth - 6) / 3);

            int col1X = SidebarWidth + 2;
            int col2X = col1X + colWidth + 2;
            int col3X = col2X + colWidth + 2;

            return (colWidth, col1X, col2X, col3X, colY, colHeight);
        }

        public void DrawDashboardLayout(Project project, int notStartedCount, int inProgressCount, int completedCount, string filter, string sort)
        {
            Console.Clear();
            DrawBoundary();
            DrawSidebarLine();

            // Header Top Bar
            string titleTag = $" Project: {ConsoleUI.Truncate(project.ProjectName, 26)} ";
            ConsoleUI.WriteAt(3, 1, titleTag, ConsoleColor.Cyan);

            int totalTasks = notStartedCount + inProgressCount + completedCount;
            string stats = $"Total: {totalTasks}  [🔴 {notStartedCount}  🟡 {inProgressCount}  🟢 {completedCount}]";
            int statsX = SidebarWidth + 3;
            ConsoleUI.WriteAt(statsX, 1, stats, ConsoleColor.Yellow);

            if (!string.IsNullOrEmpty(filter))
            {
                string filterTag = $"[Filter: \"{ConsoleUI.Truncate(filter, 12)}\"]";
                ConsoleUI.WriteAt(ConsoleUI.WindowWidth - filterTag.Length - 4, 1, filterTag, ConsoleColor.Cyan);
            }
            else
            {
                string sortTag = $"[Sort: {sort}]";
                ConsoleUI.WriteAt(ConsoleUI.WindowWidth - sortTag.Length - 4, 1, sortTag, ConsoleColor.DarkGray);
            }
        }

        public void DrawSidebarMenu(int select, bool hasFocus = true)
        {
            Console.CursorVisible = false;
            ConsoleUI.WriteAt(3, 3, "KANBAN MENU", ConsoleColor.DarkGray);

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

        public void DrawContainers(List<ToDoTask>[] columns, int selectedColumn, int[] columnTaskIndex, bool hasFocus)
        {
            var (colWidth, col1X, col2X, col3X, colY, colHeight) = GetColumnCoordinates();

            int[] colPositions = { col1X, col2X, col3X };
            string[] colTitles = { "Not started", "In Progress", "Completed" };
            ConsoleColor[] headerColors = { ConsoleColor.Red, ConsoleColor.Yellow, ConsoleColor.Green };

            for (int c = 0; c < 3; c++)
            {
                int x = colPositions[c];
                var tasks = columns[c];
                bool isColActive = (c == selectedColumn && hasFocus);
                ConsoleColor borderCol = isColActive ? ConsoleColor.Cyan : ConsoleColor.White;

                // Draw Column Outer Box
                ConsoleUI.DrawBox(x, colY, colWidth, colHeight, borderCol);

                // Column Header with Task Count
                string title = $"{colTitles[c]} ({tasks.Count})";
                int titleX = x + Math.Max(2, (colWidth - title.Length) / 2);
                ConsoleUI.WriteAt(titleX, colY - 2, title, headerColors[c]);

                // Clear interior
                ConsoleUI.ClearArea(x + 1, colY + 1, colWidth - 2, colHeight - 2);

                // Draw Tasks inside column
                if (tasks.Count == 0)
                {
                    int emptyY = colY + 3;
                    ConsoleUI.WriteAt(x + 3, emptyY, "(No tasks)", ConsoleColor.DarkGray);
                }
                else
                {
                    int maxDisplayTasks = (colHeight - 3) / 3;
                    int startIndex = Math.Max(0, columnTaskIndex[c] - maxDisplayTasks + 1);
                    int displayCount = Math.Min(tasks.Count - startIndex, maxDisplayTasks);

                    for (int i = 0; i < displayCount; i++)
                    {
                        int taskIdx = startIndex + i;
                        var task = tasks[taskIdx];
                        int itemY = colY + 1 + (i * 3);
                        bool isSelected = (isColActive && taskIdx == columnTaskIndex[c]);

                        DrawTaskCard(x + 1, itemY, colWidth - 2, task, isSelected);
                    }

                    if (tasks.Count > maxDisplayTasks)
                    {
                        string moreText = $"[ {tasks.Count} tasks total ]";
                        ConsoleUI.WriteAt(x + 2, colY + colHeight - 2, moreText, ConsoleColor.DarkGray);
                    }
                }
            }
        }

        private void DrawTaskCard(int x, int y, int width, ToDoTask task, bool isSelected)
        {
            ConsoleColor fg = isSelected ? ConsoleColor.Blue : ConsoleColor.White;
            string prefix = isSelected ? "\u25BA " : "  ";

            // Line 1: Name
            string name = ConsoleUI.Truncate(prefix + task.Name, width - 2);
            ConsoleUI.WriteAt(x + 1, y, name, fg);

            // Line 2: Assignee and due date
            string assignee = "Unassigned";
            if (task.UserId.HasValue && task.UserId.Value != Guid.Empty)
            {
                var user = _projectService.GetUserById(task.UserId.Value);
                if (user != null) assignee = user.Name;
            }

            string meta = $"  \uD83D\uDC64 {ConsoleUI.Truncate(assignee, 10)}  \uD83D\uDCC5 {task.TargetEndDate:MM-dd}";
            ConsoleUI.WriteAt(x + 1, y + 1, ConsoleUI.Truncate(meta, width - 2), ConsoleColor.DarkGray);
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

        // ================= Modals =================

        private void AddTaskModal(Project project, User? currentUser, TaskState initialStatus, ref string statusMessage, ref bool isStatusError)
        {
            var (startX, startY, width, height) = ConsoleUI.DrawModalDialog(64, 18, "ADD NEW TASK");

            DateOnly today = DateOnly.FromDateTime(DateTime.Today);
            DateOnly defaultEnd = today.AddDays(7);

            ConsoleUI.WriteAt(startX + 4, startY + 4, "Task Name        : ", ConsoleColor.White);
            ConsoleUI.DrawFieldBox(startX + 23, startY + 3, 36, ConsoleColor.White);

            ConsoleUI.WriteAt(startX + 4, startY + 7, "Description      : ", ConsoleColor.White);
            ConsoleUI.DrawFieldBox(startX + 23, startY + 6, 36, ConsoleColor.White);

            ConsoleUI.WriteAt(startX + 4, startY + 10, "Target Start Date: ", ConsoleColor.White);
            ConsoleUI.DrawFieldBox(startX + 23, startY + 9, 36, ConsoleColor.White);

            ConsoleUI.WriteAt(startX + 4, startY + 13, "Target End Date  : ", ConsoleColor.White);
            ConsoleUI.DrawFieldBox(startX + 23, startY + 12, 36, ConsoleColor.White);

            ConsoleUI.WriteAt(startX + 4, startY + 16, "[Enter] Next/Save   [Esc] Cancel", ConsoleColor.DarkCyan);

            // Read inputs
            string name = ConsoleUI.ReadTextInput(startX + 24, startY + 4, 34);
            if (string.IsNullOrWhiteSpace(name))
            {
                statusMessage = "Task creation cancelled";
                isStatusError = false;
                return;
            }

            string desc = ConsoleUI.ReadTextInput(startX + 24, startY + 7, 34);

            string startStr = ConsoleUI.ReadTextInput(startX + 24, startY + 10, 34, today.ToString("yyyy-MM-dd"));
            if (!InputValidator.IsValidDate(startStr, out DateOnly startDate))
            {
                startDate = today;
            }

            string endStr = ConsoleUI.ReadTextInput(startX + 24, startY + 13, 34, defaultEnd.ToString("yyyy-MM-dd"));
            if (!InputValidator.IsValidDate(endStr, out DateOnly endDate))
            {
                endDate = startDate.AddDays(7);
            }

            if (endDate < startDate)
            {
                statusMessage = "Target end date cannot be earlier than start date";
                isStatusError = true;
                return;
            }

            var result = _taskService.AddTask(name, desc, startDate, endDate, project.ProjectId, currentUser?.Id);
            if (result.IsSuccess)
            {
                // Update status if added in specific column
                if (initialStatus != TaskState.NotStarted)
                {
                    var addedTask = _taskService.ViewTasks(project.ProjectId).LastOrDefault(t => t.Name == name.Trim());
                    if (addedTask != null)
                    {
                        _taskService.UpdateTaskStatus(addedTask.Id, initialStatus, project.ProjectId);
                    }
                }

                statusMessage = $"Task '{name.Trim()}' added successfully!";
                isStatusError = false;
            }
            else
            {
                statusMessage = result.ErrorMessage;
                isStatusError = true;
            }
        }

        private void EditTaskModal(ToDoTask task, Project project, User? currentUser, ref string statusMessage, ref bool isStatusError)
        {
            string editedName = task.Name;
            string editedDesc = task.Description;
            string editedStartStr = task.TargetStartDate.ToString("yyyy-MM-dd");
            string editedEndStr = task.TargetEndDate.ToString("yyyy-MM-dd");

            string initialAssignee = string.Empty;
            if (task.UserId.HasValue && task.UserId.Value != Guid.Empty)
            {
                var user = _projectService.GetUserById(task.UserId.Value);
                if (user != null) initialAssignee = user.Name;
            }
            string editedAssignee = initialAssignee;

            int selectedOption = 0;
            const int optionCount = 6; // 0: Name, 1: Desc, 2: StartDate, 3: EndDate, 4: Assignee, 5: Save Changes
            string errorMessage = string.Empty;

            const int modalWidth = 72;
            const int modalHeight = 22;
            const int fieldLength = 34;

            while (true)
            {
                var (startX, startY, width, height) = ConsoleUI.DrawModalDialog(modalWidth, modalHeight, $"EDIT TASK: {ConsoleUI.Truncate(task.Name, 22)}");

                ConsoleUI.WriteAt(startX + 4, startY + 3, "Select a field to edit, then select [Save Changes]:", ConsoleColor.DarkGray);

                // Option 0: Task Name
                int opt0Y = startY + 5;
                bool isOpt0 = (selectedOption == 0);
                string prefix0 = isOpt0 ? "\u25BA " : "  ";
                ConsoleColor labelCol0 = isOpt0 ? ConsoleColor.Cyan : ConsoleColor.White;
                ConsoleColor boxCol0 = isOpt0 ? ConsoleColor.Blue : ConsoleColor.DarkGray;
                ConsoleColor textCol0 = isOpt0 ? ConsoleColor.Yellow : ConsoleColor.White;

                ConsoleUI.WriteAt(startX + 3, opt0Y, prefix0 + "Task Name        : ", labelCol0);
                ConsoleUI.WriteAt(startX + 22, opt0Y, "[ ", boxCol0);
                string dispName = ConsoleUI.Truncate(editedName, fieldLength).PadRight(fieldLength);
                ConsoleUI.WriteAt(startX + 24, opt0Y, dispName, textCol0);
                ConsoleUI.WriteAt(startX + 24 + fieldLength, opt0Y, " ]", boxCol0);

                // Option 1: Description
                int opt1Y = startY + 7;
                bool isOpt1 = (selectedOption == 1);
                string prefix1 = isOpt1 ? "\u25BA " : "  ";
                ConsoleColor labelCol1 = isOpt1 ? ConsoleColor.Cyan : ConsoleColor.White;
                ConsoleColor boxCol1 = isOpt1 ? ConsoleColor.Blue : ConsoleColor.DarkGray;
                ConsoleColor textCol1 = isOpt1 ? ConsoleColor.Yellow : ConsoleColor.White;

                ConsoleUI.WriteAt(startX + 3, opt1Y, prefix1 + "Description      : ", labelCol1);
                ConsoleUI.WriteAt(startX + 22, opt1Y, "[ ", boxCol1);
                string dispDesc = ConsoleUI.Truncate(editedDesc, fieldLength).PadRight(fieldLength);
                ConsoleUI.WriteAt(startX + 24, opt1Y, dispDesc, textCol1);
                ConsoleUI.WriteAt(startX + 24 + fieldLength, opt1Y, " ]", boxCol1);

                // Option 2: Target Start Date
                int opt2Y = startY + 9;
                bool isOpt2 = (selectedOption == 2);
                string prefix2 = isOpt2 ? "\u25BA " : "  ";
                ConsoleColor labelCol2 = isOpt2 ? ConsoleColor.Cyan : ConsoleColor.White;
                ConsoleColor boxCol2 = isOpt2 ? ConsoleColor.Blue : ConsoleColor.DarkGray;
                ConsoleColor textCol2 = isOpt2 ? ConsoleColor.Yellow : ConsoleColor.White;

                ConsoleUI.WriteAt(startX + 3, opt2Y, prefix2 + "Target Start Date: ", labelCol2);
                ConsoleUI.WriteAt(startX + 22, opt2Y, "[ ", boxCol2);
                string dispStart = ConsoleUI.Truncate(editedStartStr, fieldLength).PadRight(fieldLength);
                ConsoleUI.WriteAt(startX + 24, opt2Y, dispStart, textCol2);
                ConsoleUI.WriteAt(startX + 24 + fieldLength, opt2Y, " ]", boxCol2);

                // Option 3: Target End Date
                int opt3Y = startY + 11;
                bool isOpt3 = (selectedOption == 3);
                string prefix3 = isOpt3 ? "\u25BA " : "  ";
                ConsoleColor labelCol3 = isOpt3 ? ConsoleColor.Cyan : ConsoleColor.White;
                ConsoleColor boxCol3 = isOpt3 ? ConsoleColor.Blue : ConsoleColor.DarkGray;
                ConsoleColor textCol3 = isOpt3 ? ConsoleColor.Yellow : ConsoleColor.White;

                ConsoleUI.WriteAt(startX + 3, opt3Y, prefix3 + "Target End Date  : ", labelCol3);
                ConsoleUI.WriteAt(startX + 22, opt3Y, "[ ", boxCol3);
                string dispEnd = ConsoleUI.Truncate(editedEndStr, fieldLength).PadRight(fieldLength);
                ConsoleUI.WriteAt(startX + 24, opt3Y, dispEnd, textCol3);
                ConsoleUI.WriteAt(startX + 24 + fieldLength, opt3Y, " ]", boxCol3);

                // Option 4: Assignee
                int opt4Y = startY + 13;
                bool isOpt4 = (selectedOption == 4);
                string prefix4 = isOpt4 ? "\u25BA " : "  ";
                ConsoleColor labelCol4 = isOpt4 ? ConsoleColor.Cyan : ConsoleColor.White;
                ConsoleColor boxCol4 = isOpt4 ? ConsoleColor.Blue : ConsoleColor.DarkGray;
                ConsoleColor textCol4 = isOpt4 ? ConsoleColor.Yellow : ConsoleColor.White;

                string displayAssigneeText = string.IsNullOrWhiteSpace(editedAssignee) ? "(Unassigned)" : editedAssignee;
                ConsoleUI.WriteAt(startX + 3, opt4Y, prefix4 + "Assignee         : ", labelCol4);
                ConsoleUI.WriteAt(startX + 22, opt4Y, "[ ", boxCol4);
                string dispAssignee = ConsoleUI.Truncate(displayAssigneeText, fieldLength).PadRight(fieldLength);
                ConsoleUI.WriteAt(startX + 24, opt4Y, dispAssignee, textCol4);
                ConsoleUI.WriteAt(startX + 24 + fieldLength, opt4Y, " ]", boxCol4);

                // Option 5: Save Changes
                int opt5Y = startY + 16;
                bool isOpt5 = (selectedOption == 5);
                string prefix5 = isOpt5 ? "\u25BA " : "  ";
                ConsoleColor saveCol = isOpt5 ? ConsoleColor.Blue : ConsoleColor.Green;
                ConsoleUI.WriteAt(startX + 3, opt5Y, $"{prefix5}[ Save Changes ]", saveCol);

                // Error line
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    ConsoleUI.WriteAt(startX + 4, startY + 18, $"\u25B2 {errorMessage}", ConsoleColor.Red);
                }

                // Footer shortcuts
                ConsoleUI.WriteAt(startX + 4, startY + 20, "[↑/↓] Navigate   [Enter] Edit Field / Save   [Esc] Cancel", ConsoleColor.DarkCyan);

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
                        if (selectedOption == 0) // Edit Task Name
                        {
                            ConsoleUI.WriteAt(startX + 24, opt0Y, new string(' ', fieldLength));
                            ConsoleUI.SafeSetCursorPosition(startX + 24, opt0Y);
                            string input = ConsoleUI.ReadTextInput(startX + 24, opt0Y, fieldLength, editedName, allowEscape: true);
                            editedName = input.Trim();
                            errorMessage = string.Empty;
                        }
                        else if (selectedOption == 1) // Edit Description
                        {
                            ConsoleUI.WriteAt(startX + 24, opt1Y, new string(' ', fieldLength));
                            ConsoleUI.SafeSetCursorPosition(startX + 24, opt1Y);
                            string input = ConsoleUI.ReadTextInput(startX + 24, opt1Y, fieldLength, editedDesc, allowEscape: true);
                            editedDesc = input.Trim();
                            errorMessage = string.Empty;
                        }
                        else if (selectedOption == 2) // Edit Target Start Date
                        {
                            ConsoleUI.WriteAt(startX + 24, opt2Y, new string(' ', fieldLength));
                            ConsoleUI.SafeSetCursorPosition(startX + 24, opt2Y);
                            string input = ConsoleUI.ReadTextInput(startX + 24, opt2Y, fieldLength, editedStartStr, allowEscape: true);
                            editedStartStr = input.Trim();
                            errorMessage = string.Empty;
                        }
                        else if (selectedOption == 3) // Edit Target End Date
                        {
                            ConsoleUI.WriteAt(startX + 24, opt3Y, new string(' ', fieldLength));
                            ConsoleUI.SafeSetCursorPosition(startX + 24, opt3Y);
                            string input = ConsoleUI.ReadTextInput(startX + 24, opt3Y, fieldLength, editedEndStr, allowEscape: true);
                            editedEndStr = input.Trim();
                            errorMessage = string.Empty;
                        }
                        else if (selectedOption == 4) // Edit Assignee
                        {
                            ConsoleUI.WriteAt(startX + 24, opt4Y, new string(' ', fieldLength));
                            ConsoleUI.SafeSetCursorPosition(startX + 24, opt4Y);
                            string input = ConsoleUI.ReadTextInput(startX + 24, opt4Y, fieldLength, editedAssignee, allowEscape: true);
                            editedAssignee = input.Trim();
                            errorMessage = string.Empty;
                        }
                        else if (selectedOption == 5) // Save Changes
                        {
                            if (string.IsNullOrWhiteSpace(editedName))
                            {
                                errorMessage = "Task name cannot be empty";
                                break;
                            }

                            if (!InputValidator.IsValidDate(editedStartStr, out DateOnly startDate))
                            {
                                errorMessage = "Invalid start date format (YYYY-MM-DD)";
                                break;
                            }

                            if (!InputValidator.IsValidDate(editedEndStr, out DateOnly endDate))
                            {
                                errorMessage = "Invalid end date format (YYYY-MM-DD)";
                                break;
                            }

                            if (endDate < startDate)
                            {
                                errorMessage = "Target end date cannot be earlier than start date";
                                break;
                            }

                            Guid? newUserId = null;
                            if (!string.IsNullOrWhiteSpace(editedAssignee))
                            {
                                var targetUser = _projectService.GetUserByName(editedAssignee);
                                if (targetUser == null)
                                {
                                    errorMessage = $"User '{editedAssignee}' not found";
                                    break;
                                }
                                newUserId = targetUser.Id;
                            }

                            var result = _taskService.EditTask(task.Id, editedName, editedDesc, startDate, endDate, project.ProjectId, newUserId);
                            if (result.IsSuccess)
                            {
                                task.Name = editedName;
                                task.Description = editedDesc;
                                task.TargetStartDate = startDate;
                                task.TargetEndDate = endDate;
                                task.UserId = newUserId;

                                statusMessage = $"Task '{editedName}' updated successfully!";
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
                        bool changed = (editedName != task.Name ||
                                        editedDesc != task.Description ||
                                        editedStartStr != task.TargetStartDate.ToString("yyyy-MM-dd") ||
                                        editedEndStr != task.TargetEndDate.ToString("yyyy-MM-dd") ||
                                        editedAssignee != initialAssignee);
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

        private void TaskDetailsModal(ToDoTask task, Project project, User? currentUser, ref string statusMessage, ref bool isStatusError)
        {
            var (startX, startY, width, height) = ConsoleUI.DrawModalDialog(64, 17, $"TASK DETAILS");

            ConsoleUI.WriteAt(startX + 4, startY + 4, $"Title       : {ConsoleUI.Truncate(task.Name, 44)}", ConsoleColor.White);

            string statusStr = task.Status switch
            {
                TaskState.NotStarted => "Not Started",
                TaskState.InProgress => "In Progress",
                TaskState.Completed => "Completed",
                _ => task.Status.ToString()
            };
            ConsoleColor statusCol = task.Status switch
            {
                TaskState.NotStarted => ConsoleColor.Red,
                TaskState.InProgress => ConsoleColor.Yellow,
                TaskState.Completed => ConsoleColor.Green,
                _ => ConsoleColor.White
            };
            ConsoleUI.WriteAt(startX + 4, startY + 6, "Status      : ", ConsoleColor.White);
            ConsoleUI.WriteAt(startX + 18, startY + 6, statusStr, statusCol);

            string assignee = "Unassigned";
            if (task.UserId.HasValue && task.UserId.Value != Guid.Empty)
            {
                var user = _projectService.GetUserById(task.UserId.Value);
                if (user != null) assignee = user.Name;
            }
            ConsoleUI.WriteAt(startX + 4, startY + 8, $"Assignee    : {assignee}", ConsoleColor.White);

            ConsoleUI.WriteAt(startX + 4, startY + 10, $"Start Date  : {task.TargetStartDate:yyyy-MM-dd}", ConsoleColor.White);
            ConsoleUI.WriteAt(startX + 32, startY + 10, $"End Date : {task.TargetEndDate:yyyy-MM-dd}", ConsoleColor.White);

            string desc = string.IsNullOrWhiteSpace(task.Description) ? "(No description provided)" : task.Description;
            ConsoleUI.WriteAt(startX + 4, startY + 12, $"Description : {ConsoleUI.Truncate(desc, 44)}", ConsoleColor.Gray);

            ConsoleUI.WriteAt(startX + 4, startY + 15, "[M] Move Status   [E] Edit   [D] Delete   [Esc] Close", ConsoleColor.DarkCyan);

            while (true)
            {
                var key = Console.ReadKey(true).Key;
                if (key == ConsoleKey.Escape || key == ConsoleKey.Enter)
                {
                    break;
                }
                if (key == ConsoleKey.M)
                {
                    QuickMoveTaskModal(task, ref statusMessage, ref isStatusError);
                    break;
                }
                if (key == ConsoleKey.E)
                {
                    EditTaskModal(task, project, currentUser, ref statusMessage, ref isStatusError);
                    break;
                }
                if (key == ConsoleKey.D)
                {
                    DeleteTaskModal(task, ref statusMessage, ref isStatusError);
                    break;
                }
            }
        }

        private void QuickMoveTaskModal(ToDoTask task, ref string statusMessage, ref bool isStatusError)
        {
            var (startX, startY, width, height) = ConsoleUI.DrawModalDialog(54, 11, "MOVE TASK STATUS");

            ConsoleUI.WriteAt(startX + 4, startY + 4, $"Task: {ConsoleUI.Truncate(task.Name, 40)}", ConsoleColor.White);
            ConsoleUI.WriteAt(startX + 4, startY + 6, "Select new status:", ConsoleColor.Yellow);
            ConsoleUI.WriteAt(startX + 4, startY + 8, "[1] Not Started   [2] In Progress   [3] Completed", ConsoleColor.Cyan);

            while (true)
            {
                var key = Console.ReadKey(true).Key;
                TaskState? newState = key switch
                {
                    ConsoleKey.D1 or ConsoleKey.NumPad1 => TaskState.NotStarted,
                    ConsoleKey.D2 or ConsoleKey.NumPad2 => TaskState.InProgress,
                    ConsoleKey.D3 or ConsoleKey.NumPad3 => TaskState.Completed,
                    ConsoleKey.Escape => null,
                    _ => null
                };

                if (key == ConsoleKey.Escape) return;

                if (newState.HasValue)
                {
                    var result = _taskService.UpdateTaskStatus(task.Id, newState.Value, task.ProjectId);
                    if (result.IsSuccess)
                    {
                        statusMessage = $"Task '{task.Name}' moved to {newState.Value}.";
                        isStatusError = false;
                    }
                    else
                    {
                        statusMessage = result.ErrorMessage;
                        isStatusError = true;
                    }
                    return;
                }
            }
        }

        private void DeleteTaskModal(ToDoTask task, ref string statusMessage, ref bool isStatusError)
        {
            bool confirmed = ConsoleUI.ShowConfirmation(
                "DELETE TASK",
                $"Delete task '{ConsoleUI.Truncate(task.Name, 28)}'?"
            );

            if (confirmed)
            {
                var result = _taskService.DeleteTask(task.Id, task.ProjectId);
                if (result.IsSuccess)
                {
                    statusMessage = $"Task '{task.Name}' deleted.";
                    isStatusError = false;
                }
                else
                {
                    statusMessage = result.ErrorMessage;
                    isStatusError = true;
                }
            }
        }

        private void AssignUserModal(ToDoTask task, Project project, ref string statusMessage, ref bool isStatusError)
        {
            var (startX, startY, width, height) = ConsoleUI.DrawModalDialog(64, 15, $"ASSIGN TASK: {ConsoleUI.Truncate(task.Name, 22)}");

            var allUsers = _projectService.ViewUsers();
            ConsoleUI.WriteAt(startX + 4, startY + 4, "Available Users:", ConsoleColor.Yellow);

            if (allUsers.Count == 0)
            {
                ConsoleUI.WriteAt(startX + 6, startY + 6, "(No registered users found)", ConsoleColor.DarkGray);
            }
            else
            {
                string userList = string.Join(", ", allUsers.Select(u => u.Name));
                ConsoleUI.WriteAt(startX + 6, startY + 6, ConsoleUI.Truncate(userList, 52), ConsoleColor.White);
            }

            ConsoleUI.WriteAt(startX + 4, startY + 8, "Assign To Username : ", ConsoleColor.White);
            ConsoleUI.DrawFieldBox(startX + 25, startY + 7, 33, ConsoleColor.White);
            ConsoleUI.WriteAt(startX + 25, startY + 10, "(leave empty to unassign)", ConsoleColor.DarkGray);

            ConsoleUI.WriteAt(startX + 4, startY + 13, "[Enter] Assign   [Esc] Cancel", ConsoleColor.DarkCyan);

            string username = ConsoleUI.ReadTextInput(startX + 26, startY + 8, 30);
            if (string.IsNullOrWhiteSpace(username))
            {
                _taskService.AssignUser(task.Id, null);
                statusMessage = $"Task '{task.Name}' unassigned.";
                isStatusError = false;
                return;
            }

            var targetUser = _projectService.GetUserByName(username);
            if (targetUser == null)
            {
                statusMessage = $"User '{username}' not found";
                isStatusError = true;
                return;
            }

            var result = _taskService.AssignUser(task.Id, targetUser.Id);
            if (result.IsSuccess)
            {
                statusMessage = $"Task assigned to @{targetUser.Name}!";
                isStatusError = false;
            }
            else
            {
                statusMessage = result.ErrorMessage;
                isStatusError = true;
            }
        }

        private void FilterTasksModal(ref string searchFilter, ref string statusMessage)
        {
            var (startX, startY, width, height) = ConsoleUI.DrawModalDialog(54, 11, "FILTER TASKS");

            ConsoleUI.WriteAt(startX + 4, startY + 4, "Keyword : ", ConsoleColor.White);
            ConsoleUI.DrawFieldBox(startX + 14, startY + 3, 34, ConsoleColor.White);

            ConsoleUI.WriteAt(startX + 4, startY + 7, "[Enter] Apply Filter   [Esc] Clear & Exit", ConsoleColor.DarkCyan);

            string query = ConsoleUI.ReadTextInput(startX + 15, startY + 4, 32, searchFilter);
            searchFilter = query.Trim();
            statusMessage = string.IsNullOrWhiteSpace(searchFilter) ? "Filter cleared" : $"Filtered by: \"{searchFilter}\"";
        }
    }
}
