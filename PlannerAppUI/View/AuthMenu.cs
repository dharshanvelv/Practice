using System;
using System.Linq;
using System.Threading;
using PlannerApplication.Models;
using PlannerApplication.Services;
using PlannerApplication.Utility;

namespace PlannerApplication.View
{
    public class AuthMenu
    {
        private readonly AuthService _authService;
        private readonly string[] menu = { "Login", "Signup", "Forgot Password" };

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

                if (select == menu.Length || select == -99) // Escape requested
                {
                    bool confirmed = ConsoleUI.ShowConfirmation(
                        "Exit Application",
                        "Are you sure you want to exit PlannerApp?"
                    );

                    if (confirmed)
                    {
                        return null;
                    }
                    continue;
                }

                if (select == 0) // Login
                {
                    // If currently locked out, keep on login screen and show live countdown
                    if (_authService.IsLockedOut)
                    {
                        bool exitRequested = HandleLockoutCountdown();
                        if (exitRequested)
                        {
                            return null;
                        }
                        continue;
                    }

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
                        // Check if this failure triggered a lockout
                        if (_authService.IsLockedOut)
                        {
                            bool exitRequested = HandleLockoutCountdown();
                            if (exitRequested)
                            {
                                return null;
                            }
                        }
                        else
                        {
                            statusMessage = loginResult.ErrorMessage;
                            isStatusError = true;
                        }
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
                        // Display the generated 6-digit recovery code clearly
                        DisplayRecoveryCodeModal(username.Trim(), signupResult.RecoveryCode);

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
                else if (select == 2) // Forgot Password
                {
                    bool recovered = DisplayForgotPasswordModal();
                    if (recovered)
                    {
                        statusMessage = "Password reset successful! You can now log in with your new password.";
                        isStatusError = false;
                        select = 0; // Automatically switch to Login
                    }
                    else
                    {
                        statusMessage = "Password recovery cancelled or failed.";
                        isStatusError = false;
                    }
                }
            }

            return null;
        }

        private bool HandleLockoutCountdown()
        {
            Console.CursorVisible = false;
            while (_authService.IsLockedOut)
            {
                int remaining = _authService.RemainingLockoutSeconds;
                if (remaining <= 0) break;

                string lockoutMsg = $"[LOCKED OUT] Maximum attempts exceeded. Retry in {remaining}s... [Esc] Exit";
                ShowStatus(lockoutMsg, isError: true);

                // Wait in 100ms intervals to remain responsive to Esc key
                for (int i = 0; i < 10; i++)
                {
                    if (!_authService.IsLockedOut) break;

                    if (Console.KeyAvailable)
                    {
                        var keyInfo = Console.ReadKey(true);
                        if (keyInfo.Key == ConsoleKey.Escape)
                        {
                            bool confirmed = ConsoleUI.ShowConfirmation(
                                "Exit Application",
                                "Are you sure you want to exit PlannerApp?"
                            );

                            if (confirmed)
                            {
                                return true; // Exit application
                            }

                            // User cancelled exit; return to countdown and redraw auth layout
                            Console.Clear();
                            DrawAuthLayout();
                            DrawTitleBanner();
                            DrawMenu(0);
                            break;
                        }
                    }
                    Thread.Sleep(100);
                }
            }

            // Lockout countdown expired: automatically re-enable login
            _authService.ResetLockout();
            Console.Clear();
            DrawAuthLayout();
            DrawTitleBanner();
            DrawMenu(0);
            ShowStatus("Lockout expired. Login is now re-enabled.", isError: false);
            return false;
        }

        private void DisplayRecoveryCodeModal(string username, string recoveryCode)
        {
            var (startX, startY, width, height) = ConsoleUI.DrawModalDialog(64, 15, "ACCOUNT RECOVERY CODE");

            ConsoleUI.WriteAt(startX + 4, startY + 3, $"Welcome, {username}! Your account has been created.", ConsoleColor.Green);
            ConsoleUI.WriteAt(startX + 4, startY + 5, "YOUR 6-DIGIT RECOVERY CODE:", ConsoleColor.Yellow);

            int codeX = startX + (width - 18) / 2;
            ConsoleUI.DrawFieldBox(codeX, startY + 6, 18, ConsoleColor.Cyan);
            ConsoleUI.WriteAt(codeX + 6, startY + 7, recoveryCode, ConsoleColor.Cyan);

            ConsoleUI.WriteAt(startX + 4, startY + 9, "IMPORTANT: Please save this recovery code securely!", ConsoleColor.Red);
            ConsoleUI.WriteAt(startX + 4, startY + 10, "You will need it to recover your account if you forget", ConsoleColor.White);
            ConsoleUI.WriteAt(startX + 4, startY + 11, "your password. It will not be shown again.", ConsoleColor.White);

            ConsoleUI.WriteAt(startX + 4, startY + 13, "Press [Enter] to continue to Login...", ConsoleColor.DarkCyan);

            while (true)
            {
                var key = Console.ReadKey(true).Key;
                if (key == ConsoleKey.Enter || key == ConsoleKey.Escape)
                {
                    break;
                }
            }
        }

        private bool DisplayForgotPasswordModal()
        {
            var (startX, startY, width, height) = ConsoleUI.DrawModalDialog(64, 18, "FORGOT PASSWORD");

            ConsoleUI.WriteAt(startX + 4, startY + 3, "Verify your identity using your 6-digit recovery code:", ConsoleColor.Yellow);

            ConsoleUI.WriteAt(startX + 4, startY + 5, "Username      : ", ConsoleColor.White);
            ConsoleUI.DrawFieldBox(startX + 20, startY + 4, 38, ConsoleColor.White);

            ConsoleUI.WriteAt(startX + 4, startY + 8, "Recovery Code : ", ConsoleColor.White);
            ConsoleUI.DrawFieldBox(startX + 20, startY + 7, 16, ConsoleColor.White);
            ConsoleUI.WriteAt(startX + 38, startY + 8, "(6 digits)", ConsoleColor.DarkGray);

            ConsoleUI.WriteAt(startX + 4, startY + 16, "[Enter] Next   [Esc] Cancel", ConsoleColor.DarkCyan);

            // Read username
            string username = ConsoleUI.ReadTextInput(startX + 21, startY + 5, 36);
            if (string.IsNullOrWhiteSpace(username))
            {
                return false;
            }

            // Read recovery code
            string recoveryCode = ConsoleUI.ReadTextInput(startX + 21, startY + 8, 14);
            if (string.IsNullOrWhiteSpace(recoveryCode))
            {
                return false;
            }

            var verifyResult = _authService.VerifyRecoveryCode(username, recoveryCode);
            if (!verifyResult.IsSuccess)
            {
                ConsoleUI.WriteAt(startX + 4, startY + 11, verifyResult.ErrorMessage, ConsoleColor.Red);
                ConsoleUI.WriteAt(startX + 4, startY + 13, "Press any key to return...", ConsoleColor.DarkGray);
                Console.ReadKey(true);
                return false;
            }

            // Identity verified: Prompt for new password
            ConsoleUI.ClearArea(startX + 2, startY + 3, width - 4, height - 5);
            ConsoleUI.WriteAt(startX + 4, startY + 3, "Identity verified! Set and confirm your new password:", ConsoleColor.Green);

            ConsoleUI.WriteAt(startX + 4, startY + 6, "New Password  : ", ConsoleColor.White);
            ConsoleUI.DrawFieldBox(startX + 20, startY + 5, 38, ConsoleColor.White);

            ConsoleUI.WriteAt(startX + 4, startY + 9, "Confirm Pass  : ", ConsoleColor.White);
            ConsoleUI.DrawFieldBox(startX + 20, startY + 8, 38, ConsoleColor.White);

            ConsoleUI.WriteAt(startX + 4, startY + 16, "[Enter] Save Changes   [Esc] Cancel", ConsoleColor.DarkCyan);

            string? newPass = ConsoleUI.ReadMaskedPassword(startX + 21, startY + 6, 36);
            if (string.IsNullOrWhiteSpace(newPass))
            {
                return false;
            }

            string? confirmPass = ConsoleUI.ReadMaskedPassword(startX + 21, startY + 9, 36);
            if (string.IsNullOrWhiteSpace(confirmPass))
            {
                return false;
            }

            if (newPass != confirmPass)
            {
                ConsoleUI.WriteAt(startX + 4, startY + 12, "Passwords do not match.", ConsoleColor.Red);
                ConsoleUI.WriteAt(startX + 4, startY + 14, "Press any key to return...", ConsoleColor.DarkGray);
                Console.ReadKey(true);
                return false;
            }

            var resetResult = _authService.ResetPassword(username, recoveryCode, newPass);
            if (!resetResult.IsSuccess)
            {
                ConsoleUI.WriteAt(startX + 4, startY + 12, resetResult.ErrorMessage, ConsoleColor.Red);
                ConsoleUI.WriteAt(startX + 4, startY + 14, "Press any key to return...", ConsoleColor.DarkGray);
                Console.ReadKey(true);
                return false;
            }

            return true;
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
            int totalLen = menu.Sum(m => m.Length + 4) + (menu.Length - 1) * 3;
            int startX = Math.Max(2, (ConsoleUI.WindowWidth - totalLen) / 2);
            int startY = ConsoleUI.WindowHeight / 2 - 5;

            ConsoleUI.SafeSetCursorPosition(2, startY);
            Console.Write(new string(' ', ConsoleUI.WindowWidth - 4));
            ConsoleUI.SafeSetCursorPosition(startX, startY);

            for (int i = 0; i < menu.Length; i++)
            {
                if (i == select)
                {
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.Write($"[{menu[i]}]   ");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.Write($" {menu[i]}    ");
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
                        return menu.Length; // Escape sentinel
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
