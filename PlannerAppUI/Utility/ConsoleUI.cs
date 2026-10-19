using System;
using System.Text;

namespace PlannerApplication.Utility
{
    public static class ConsoleUI
    {
        // User's preferred single-line box drawing characters
        public const char TopLeft = '\u250C';     // ┌
        public const char TopRight = '\u2510';    // ┐
        public const char BottomLeft = '\u2514'; // └
        public const char BottomRight = '\u2518';// ┘
        public const char Horizontal = '\u2500'; // ─
        public const char Vertical = '\u2502';   // │
        public const char TDown = '\u252C';      // ┬
        public const char TUp = '\u2534';        // ┴
        public const char TRight = '\u251C';     // ├
        public const char TLeft = '\u2524';      // ┤
        public const char Cross = '\u253C';      // ┼

        public static int WindowWidth
        {
            get
            {
                try { return Math.Max(Console.WindowWidth, 80); }
                catch { return 100; }
            }
        }

        public static int WindowHeight
        {
            get
            {
                try { return Math.Max(Console.WindowHeight, 25); }
                catch { return 30; }
            }
        }

        public static void InitializeConsole()
        {
            try
            {
                Console.OutputEncoding = Encoding.UTF8;
                Console.InputEncoding = Encoding.UTF8;
                Console.Title = "PlannerApp - Project & Task Management";
                Console.CursorVisible = false;
            }
            catch
            {
                // Ignore if console buffer cannot be modified
            }
        }

        public static void SafeSetCursorPosition(int x, int y)
        {
            try
            {
                int safeX = Math.Clamp(x, 0, Math.Max(0, Console.WindowWidth - 1));
                int safeY = Math.Clamp(y, 0, Math.Max(0, Console.WindowHeight - 1));
                Console.SetCursorPosition(safeX, safeY);
            }
            catch
            {
                // Ignore if terminal cursor position is invalid
            }
        }

        public static void WriteAt(int x, int y, string text, ConsoleColor? fg = null, ConsoleColor? bg = null)
        {
            if (string.IsNullOrEmpty(text)) return;
            try
            {
                SafeSetCursorPosition(x, y);

                ConsoleColor prevFg = Console.ForegroundColor;
                ConsoleColor prevBg = Console.BackgroundColor;

                if (fg.HasValue) Console.ForegroundColor = fg.Value;
                if (bg.HasValue) Console.BackgroundColor = bg.Value;

                // Ensure text doesn't overflow line
                int maxLen = Math.Max(0, Console.WindowWidth - x - 1);
                if (text.Length > maxLen)
                {
                    text = text.Substring(0, maxLen);
                }

                Console.Write(text);

                if (fg.HasValue) Console.ForegroundColor = prevFg;
                if (bg.HasValue) Console.BackgroundColor = prevBg;
            }
            catch
            {
                // Safeguard against console writes
            }
        }

        public static void DrawBoundary(ConsoleColor color = ConsoleColor.White)
        {
            int w = WindowWidth;
            int h = WindowHeight;

            string horline = new string(Horizontal, Math.Max(0, w - 3));
            ConsoleColor prevFg = Console.ForegroundColor;
            Console.ForegroundColor = color;

            SafeSetCursorPosition(1, 1);
            Console.Write($"{TopLeft}{horline}{TopRight}");

            for (int i = 2; i < h - 1; i++)
            {
                SafeSetCursorPosition(1, i);
                Console.Write(Vertical);
                SafeSetCursorPosition(w - 1, i);
                Console.Write(Vertical);
            }

            SafeSetCursorPosition(1, h - 1);
            Console.Write($"{BottomLeft}{horline}{BottomRight}");

            Console.ForegroundColor = prevFg;
        }

        public static void DrawBox(int x, int y, int width, int height, ConsoleColor color = ConsoleColor.White)
        {
            if (width < 2 || height < 2) return;
            ConsoleColor prev = Console.ForegroundColor;
            Console.ForegroundColor = color;

            string horLine = new string(Horizontal, width - 2);

            WriteAt(x, y, $"{TopLeft}{horLine}{TopRight}", color);

            for (int r = 1; r < height - 1; r++)
            {
                WriteAt(x, y + r, Vertical.ToString(), color);
                WriteAt(x + width - 1, y + r, Vertical.ToString(), color);
            }

            WriteAt(x, y + height - 1, $"{BottomLeft}{horLine}{BottomRight}", color);
            Console.ForegroundColor = prev;
        }

        public static void DrawFieldBox(int x, int y, int length, ConsoleColor color = ConsoleColor.White)
        {
            string line = new string(Horizontal, length);
            string blankline = new string(' ', length);

            WriteAt(x, y, $"{TopLeft}{line}{TopRight}", color);
            WriteAt(x, y + 1, $"{Vertical}{blankline}{Vertical}", color);
            WriteAt(x, y + 2, $"{BottomLeft}{line}{BottomRight}", color);
        }

        public static void ClearArea(int x, int y, int width, int height)
        {
            string blank = new string(' ', Math.Max(0, width));
            for (int r = 0; r < height; r++)
            {
                WriteAt(x, y + r, blank);
            }
        }

        public static string Truncate(string? text, int maxLength)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            if (maxLength <= 0) return string.Empty;
            if (text.Length <= maxLength) return text;
            if (maxLength <= 3) return text.Substring(0, maxLength);
            return text.Substring(0, maxLength - 3) + "...";
        }

        public static string ReadTextInput(int x, int y, int maxLength, string initialValue = "", bool allowEscape = true)
        {
            Console.CursorVisible = true;
            StringBuilder buffer = new StringBuilder(initialValue);

            // Clear input box line
            WriteAt(x, y, new string(' ', maxLength));
            WriteAt(x, y, buffer.ToString());

            int cursorIndex = buffer.Length;
            SafeSetCursorPosition(x + cursorIndex, y);

            while (true)
            {
                ConsoleKeyInfo keyInfo = Console.ReadKey(true);

                if (keyInfo.Key == ConsoleKey.Enter)
                {
                    Console.CursorVisible = false;
                    return buffer.ToString();
                }

                if (keyInfo.Key == ConsoleKey.Escape && allowEscape)
                {
                    Console.CursorVisible = false;
                    return initialValue;
                }

                if (keyInfo.Key == ConsoleKey.Backspace)
                {
                    if (cursorIndex > 0)
                    {
                        buffer.Remove(cursorIndex - 1, 1);
                        cursorIndex--;
                        WriteAt(x, y, buffer.ToString().PadRight(maxLength));
                        SafeSetCursorPosition(x + cursorIndex, y);
                    }
                    continue;
                }

                if (keyInfo.Key == ConsoleKey.LeftArrow)
                {
                    if (cursorIndex > 0)
                    {
                        cursorIndex--;
                        SafeSetCursorPosition(x + cursorIndex, y);
                    }
                    continue;
                }

                if (keyInfo.Key == ConsoleKey.RightArrow)
                {
                    if (cursorIndex < buffer.Length)
                    {
                        cursorIndex++;
                        SafeSetCursorPosition(x + cursorIndex, y);
                    }
                    continue;
                }

                if (!char.IsControl(keyInfo.KeyChar))
                {
                    if (buffer.Length < maxLength)
                    {
                        buffer.Insert(cursorIndex, keyInfo.KeyChar);
                        cursorIndex++;
                        WriteAt(x, y, buffer.ToString().PadRight(maxLength));
                        SafeSetCursorPosition(x + cursorIndex, y);
                    }
                }
            }
        }

        public static string? ReadMaskedPassword(int x, int y, int maxLength)
        {
            Console.CursorVisible = true;
            StringBuilder buffer = new StringBuilder();

            WriteAt(x, y, new string(' ', maxLength));
            SafeSetCursorPosition(x, y);

            while (true)
            {
                ConsoleKeyInfo keyInfo = Console.ReadKey(true);

                if (keyInfo.Key == ConsoleKey.Enter)
                {
                    Console.CursorVisible = false;
                    return buffer.ToString();
                }

                if (keyInfo.Key == ConsoleKey.Escape)
                {
                    Console.CursorVisible = false;
                    return null;
                }

                if (keyInfo.Key == ConsoleKey.Backspace)
                {
                    if (buffer.Length > 0)
                    {
                        buffer.Remove(buffer.Length - 1, 1);
                        WriteAt(x, y, new string('*', buffer.Length).PadRight(maxLength));
                        SafeSetCursorPosition(x + buffer.Length, y);
                    }
                    continue;
                }

                if (!char.IsControl(keyInfo.KeyChar))
                {
                    if (buffer.Length < maxLength)
                    {
                        buffer.Append(keyInfo.KeyChar);
                        WriteAt(x, y, new string('*', buffer.Length).PadRight(maxLength));
                        SafeSetCursorPosition(x + buffer.Length, y);
                    }
                }
            }
        }

        public static (int startX, int startY, int width, int height) DrawModalDialog(int width, int height, string title)
        {
            int startX = Math.Max(2, (WindowWidth - width) / 2);
            int startY = Math.Max(2, (WindowHeight - height) / 2);

            ClearArea(startX, startY, width, height);
            DrawBox(startX, startY, width, height, ConsoleColor.Cyan);

            // Draw header bar
            string titlePadded = $" {title} ";
            int titleX = startX + Math.Max(2, (width - titlePadded.Length) / 2);
            WriteAt(titleX, startY, titlePadded, ConsoleColor.Yellow);

            // Horizontal divider below title
            string divider = new string(Horizontal, width - 2);
            WriteAt(startX + 1, startY + 2, divider, ConsoleColor.DarkGray);

            return (startX, startY, width, height);
        }

        public static bool ShowConfirmation(string title, string message, string confirmText = "Yes [Y]", string cancelText = "No [N]")
        {
            int width = Math.Max(50, Math.Max(title.Length, message.Length) + 8);
            int height = 9;

            var (startX, startY, _, _) = DrawModalDialog(width, height, title);

            WriteAt(startX + 4, startY + 4, message, ConsoleColor.White);

            string buttons = $"[Y] {confirmText}       [N] {cancelText}";
            int btnX = startX + Math.Max(4, (width - buttons.Length) / 2);
            WriteAt(btnX, startY + 6, buttons, ConsoleColor.Blue);

            while (true)
            {
                var key = Console.ReadKey(true).Key;
                if (key == ConsoleKey.Y || key == ConsoleKey.Enter)
                {
                    return true;
                }
                if (key == ConsoleKey.N || key == ConsoleKey.Escape)
                {
                    return false;
                }
            }
        }

        public static void ShowStatusBar(string shortcutHelp, string? statusMessage = null, bool isError = false)
        {
            int yShortcuts = WindowHeight - 3;
            int yMessage = WindowHeight - 2;
            int w = WindowWidth;

            // Clear status lines
            WriteAt(3, yShortcuts, new string(' ', w - 6));
            WriteAt(3, yMessage, new string(' ', w - 6));

            if (!string.IsNullOrEmpty(shortcutHelp))
            {
                WriteAt(3, yShortcuts, shortcutHelp, ConsoleColor.DarkCyan);
            }

            if (!string.IsNullOrEmpty(statusMessage))
            {
                ConsoleColor msgColor = isError ? ConsoleColor.Red : ConsoleColor.Green;
                string prefix = isError ? "▲ " : "✔ ";
                WriteAt(3, yMessage, prefix + statusMessage, msgColor);
            }
        }
    }
}
