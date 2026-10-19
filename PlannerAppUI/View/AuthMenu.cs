using System;
using System.Threading;
using PlannerApplication.Models;
using PlannerApplication.Services;
using PlannerApplication.Utility;

namespace PlannerApplication.View
{
    public class AuthMenu
    {
        private readonly AuthService _authService;
        private readonly string[] menu = { "Login", "Signup" };

        public AuthMenu(AuthService? authService = null)
        {
            _authService = authService ?? new AuthService();
        }

        private int UsernameCursorPosX => ConsoleUI.WindowWidth / 2 - 9;
        private int UsernameCursorPosY => ConsoleUI.WindowHeight / 2 - 2;
        private int PasswordCursorPosX => ConsoleUI.WindowWidth / 2 - 9;
        private int PasswordCursorPosY => ConsoleUI.WindowHeight / 2 + 2;

        public User? DisplayAuthPage()
        {
            bool isAppRunning = true;
            int select = 0;
            string statusMessage = string.Empty;
            bool isStatusError = false;

            while (isAppRunning)
            {
                Console.Clear();
                DrawAuthLayout();
                DrawTitleBanner();

                if (!string.IsNullOrEmpty(statusMessage))
                {
                    ShowStatus(statusMessage, isStatusError);
                    statusMessage = string.Empty;
                }
                else
                {
                    ShowStatus("Use [←/→] to switch, [Enter] to select, [Esc] to exit", false);
                }

                select = DrawMenuAndReturnSelection(select);

                if (select == 3 || select == -99) // Escape requested
                {
                    ConsoleUI.SafeSetCursorPosition(ConsoleUI.WindowWidth / 2 - 16, ConsoleUI.WindowHeight - 4);
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("Press Enter to confirm Exit, or Esc to cancel");
                    Console.ResetColor();

                    var key = Console.ReadKey(true).Key;
                    if (key == ConsoleKey.Enter)
                    {
                        return null;
                    }
                    continue;
                }

                if (select == 0) // Login
                {
                    ShowStatus("Enter username and password (or Esc to cancel)", false);

                    // Username input
                    ConsoleUI.SafeSetCursorPosition(UsernameCursorPosX, UsernameCursorPosY);
                    string username = ConsoleUI.ReadTextInput(UsernameCursorPosX, UsernameCursorPosY, 28);
                    if (string.IsNullOrWhiteSpace(username))
                    {
                        statusMessage = "Login cancelled";
                        isStatusError = false;
                        continue;
                    }

                    // Password input (masked)
                    ConsoleUI.SafeSetCursorPosition(PasswordCursorPosX, PasswordCursorPosY);
                    string? password = ConsoleUI.ReadMaskedPassword(PasswordCursorPosX, PasswordCursorPosY, 28);
                    if (password == null)
                    {
                        statusMessage = "Login cancelled";
                        isStatusError = false;
                        continue;
                    }

                    var loginResult = _authService.LoginUser(username, password);
                    if (loginResult.IsSuccess)
                    {
                        ShowStatus($"Login successful! Welcome, {loginResult.UserName}.", false);
                        Thread.Sleep(600);
                        return new User { Id = loginResult.Id, Name = loginResult.UserName };
                    }
                    else
                    {
                        statusMessage = loginResult.ErrorMessage;
                        isStatusError = true;
                    }
                }
                else if (select == 1) // Signup
                {
                    ShowStatus("Create a new account (Username: min 3 chars, Password: min 4 chars)", false);

                    // Username input
                    ConsoleUI.SafeSetCursorPosition(UsernameCursorPosX, UsernameCursorPosY);
                    string username = ConsoleUI.ReadTextInput(UsernameCursorPosX, UsernameCursorPosY, 28);
                    if (string.IsNullOrWhiteSpace(username))
                    {
                        statusMessage = "Signup cancelled";
                        isStatusError = false;
                        continue;
                    }

                    // Password input (masked)
                    ConsoleUI.SafeSetCursorPosition(PasswordCursorPosX, PasswordCursorPosY);
                    string? password = ConsoleUI.ReadMaskedPassword(PasswordCursorPosX, PasswordCursorPosY, 28);
                    if (password == null)
                    {
                        statusMessage = "Signup cancelled";
                        isStatusError = false;
                        continue;
                    }

                    var signupResult = _authService.AddUser(username, password);
                    if (signupResult.IsSuccess)
                    {
                        statusMessage = "Signup successful! You can now log in.";
                        isStatusError = false;
                        select = 0; // Automatically switch to Login
                    }
                    else
                    {
                        statusMessage = signupResult.ErrorMessage;
                        isStatusError = true;
                    }
                }
            }

            return null;
        }

        private void DrawTitleBanner()
        {
            string title = "P L A N N E R   A P P";
            string subtitle = "Console Project & Task Dashboard";

            int titleX = Math.Max(2, (ConsoleUI.WindowWidth - title.Length) / 2);
            int subX = Math.Max(2, (ConsoleUI.WindowWidth - subtitle.Length) / 2);

            ConsoleUI.WriteAt(titleX, 3, title, ConsoleColor.Cyan);
            ConsoleUI.WriteAt(subX, 4, subtitle, ConsoleColor.DarkGray);
        }

        public void DrawMenu(int select)
        {
            Console.CursorVisible = false;
            int startX = ConsoleUI.WindowWidth / 2 - 12;
            int startY = ConsoleUI.WindowHeight / 2 - 5;

            ConsoleUI.SafeSetCursorPosition(startX, startY);
            Console.Write(new string(' ', 30));
            ConsoleUI.SafeSetCursorPosition(ConsoleUI.WindowWidth / 2 - 8, startY);

            for (int i = 0; i < menu.Length; i++)
            {
                if (i == select)
                {
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.Write($"[{menu[i]}]  ");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.Write($" {menu[i]}   ");
                    Console.ResetColor();
                }
            }
        }

        public int DrawMenuAndReturnSelection(int initialSelection = 0)
        {
            Console.CursorVisible = false;
            int select = Math.Clamp(initialSelection, 0, menu.Length - 1);
            DrawMenu(select);

            while (true)
            {
                var keyInfo = Console.ReadKey(true);
                switch (keyInfo.Key)
                {
                    case ConsoleKey.RightArrow:
                        if (select < menu.Length - 1)
                        {
                            select++;
                            DrawMenu(select);
                        }
                        break;

                    case ConsoleKey.LeftArrow:
                        if (select > 0)
                        {
                            select--;
                            DrawMenu(select);
                        }
                        break;

                    case ConsoleKey.DownArrow:
                    case ConsoleKey.Enter:
                        Console.CursorVisible = true;
                        return select;

                    case ConsoleKey.Escape:
                        return 3;
                }
            }
        }

        public void DrawUsernameBox()
        {
            int labelX = ConsoleUI.WindowWidth / 2 - 20;
            int labelY = ConsoleUI.WindowHeight / 2 - 2;
            int boxX = ConsoleUI.WindowWidth / 2 - 10;
            int boxY = ConsoleUI.WindowHeight / 2 - 3;

            ConsoleUI.WriteAt(labelX, labelY, "Username : ", ConsoleColor.White);
            ConsoleUI.DrawFieldBox(boxX, boxY, 30, ConsoleColor.White);
        }

        public void DrawPasswordBox()
        {
            int labelX = ConsoleUI.WindowWidth / 2 - 20;
            int labelY = ConsoleUI.WindowHeight / 2 + 2;
            int boxX = ConsoleUI.WindowWidth / 2 - 10;
            int boxY = ConsoleUI.WindowHeight / 2 + 1;

            ConsoleUI.WriteAt(labelX, labelY, "Password : ", ConsoleColor.White);
            ConsoleUI.DrawFieldBox(boxX, boxY, 30, ConsoleColor.White);
        }

        public void DrawFieldBox(int length)
        {
            var curPosition = Console.GetCursorPosition();
            ConsoleUI.DrawFieldBox(curPosition.Left, curPosition.Top, length);
        }

        public void DrawBoundary()
        {
            ConsoleUI.DrawBoundary();
        }

        public void DrawAuthLayout()
        {
            DrawBoundary();
            DrawUsernameBox();
            DrawPasswordBox();
        }

        private void ShowStatus(string message, bool isError)
        {
            int y = ConsoleUI.WindowHeight - 4;
            ConsoleUI.SafeSetCursorPosition(2, y);
            Console.Write(new string(' ', ConsoleUI.WindowWidth - 4));

            int x = Math.Max(2, (ConsoleUI.WindowWidth - message.Length) / 2);
            ConsoleColor color = isError ? ConsoleColor.Red : ConsoleColor.Green;
            ConsoleUI.WriteAt(x, y, message, color);
        }
    }
}
