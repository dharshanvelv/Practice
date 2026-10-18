using System;
using System.Collections.Generic;
using System.Text;

namespace PlannerAppUI
{
    public class AuthMenu
    {
        
        string[] menu = { "Login", "Signup" };
        int usernameCursorPosX = Console.WindowWidth / 2 - 9;
        int usernameCursorPosY = Console.WindowHeight / 2 - 2;
        int passwordCursorPosX = Console.WindowWidth / 2 - 9;
        int passwordCursorPosY = Console.WindowHeight / 2 + 2;

        public void DisplayAuthPage()
        {
            DrawAuthLayout();
            bool isAppRunning = true;
            while (isAppRunning)
            {
                int choice = DrawMenuAndReturnSelection();
                switch (choice)
                {
                    case 0:
                        // Handle Login selection
                        Console.SetCursorPosition(usernameCursorPosX, usernameCursorPosY);
                        string username = Console.ReadLine();
                        Console.SetCursorPosition(passwordCursorPosX, passwordCursorPosY);
                        string password = Console.ReadLine();
                        Console.SetCursorPosition(Console.WindowWidth / 2, Console.WindowHeight - 4);
                        Console.WriteLine("Login successful!");
                        break;
                    case 1:
                        // Handle Signup selection
                        Console.SetCursorPosition(usernameCursorPosX, usernameCursorPosY);
                        string username1 = Console.ReadLine();
                        Console.SetCursorPosition(passwordCursorPosX, passwordCursorPosY);
                        string password1 = Console.ReadLine();
                        Console.SetCursorPosition(Console.WindowWidth / 2, Console.WindowHeight - 4);
                        Console.WriteLine("Signup successful!");
                        break;

                    case 3:
                        Console.SetCursorPosition(Console.WindowWidth / 2, Console.WindowHeight - 4);
                        Console.Write("Press enter to confirm Exit");
                        if(Console.ReadKey().Key == ConsoleKey.Enter)
                        {
                            isAppRunning = false;
                            break;
                        }
                        break;

                    default:
                        break;
                }
            }
        }
        
        public void DrawMenu(int select)
        {
            Console.CursorVisible = false;
            Console.SetCursorPosition(Console.WindowWidth / 2 - 10, Console.WindowHeight / 2 - 5);
            Console.Write("                           ");
            Console.SetCursorPosition(Console.WindowWidth / 2 - 5, Console.WindowHeight / 2 - 5);

            for (int i = 0; i < menu.Length; i++)
            {
                if (i == select)
                {
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.Write(menu[i]+" ");
                    Console.ResetColor();
                    continue;
                }
                Console.Write(menu[i]+" ");
            }
        }
        public int DrawMenuAndReturnSelection()
        {
            Console.CursorVisible = false;
            DrawMenu(0);
            int select = 0;
            bool EnterNotPressed = true;

            //Menu selection logic loop
            while (EnterNotPressed)
            { 
                switch (Console.ReadKey(true).Key)
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

                    case ConsoleKey.Enter:
                        EnterNotPressed = false;
                        Console.CursorVisible = true;
                        break;

                    case ConsoleKey.Escape:
                        select = 3;
                        EnterNotPressed = false;
                        break;
                }
            }

            //    if (select == 0)
            //    {
            //        if (Console.ReadKey().Key == ConsoleKey.RightArrow)
            //        {
            //            select = 1;
            //            DrawMenu(select);
            //            if (Console.ReadKey().Key == ConsoleKey.Enter)
            //            {
            //                EnterNotPressed = false;
            //                break;
            //            }
            //        }
            //    }

            //    else if (select == 1)
            //    {
            //        if (Console.ReadKey().Key == ConsoleKey.LeftArrow)
            //        {
            //            select = 0;
            //            DrawMenu(select);
            //            if (Console.ReadKey().Key == ConsoleKey.Enter)
            //            {
            //                EnterNotPressed = false;
            //                break;
            //            }
            //        }
            //    }             
            //}

            return select;
        }

        public void DrawUsernameBox()
        {
            Console.SetCursorPosition(Console.WindowWidth / 2 - 20, Console.WindowHeight / 2 - 2);
            Console.Write("Username : ");
            Console.SetCursorPosition(Console.WindowWidth / 2 - 10, Console.WindowHeight / 2 - 3);
            DrawFieldBox(30);
            Console.SetCursorPosition(0, 0);
        }

        public void DrawPasswordBox()
        {
            Console.SetCursorPosition(Console.WindowWidth / 2 - 20, Console.WindowHeight / 2 + 2);
            Console.Write("Password : ");
            Console.SetCursorPosition(Console.WindowWidth / 2 - 10, Console.WindowHeight / 2 + 1);
            DrawFieldBox(30);
            Console.SetCursorPosition(0, 0);
        }

        public void DrawFieldBox(int length)
        {
            string line = new string('\u2500', length);
            string blankline = new string(' ', length);
            var curPosition = Console.GetCursorPosition();
            Console.Write($"\u250C{line}\u2510");
            Console.SetCursorPosition(curPosition.Item1, curPosition.Item2 + 1);
            Console.Write($"\u2502{blankline}\u2502");
            Console.SetCursorPosition(curPosition.Item1, curPosition.Item2 + 2);
            Console.Write($"\u2514{line}\u2518");
        }

        public void DrawBoundary()
        {
            string horline = new string('\u2500', Console.WindowWidth - 3);
            Console.SetCursorPosition(1, 1);
            Console.Write($"\u250C{horline}\u2510");
            for (int i = 2; i < Console.WindowHeight - 1; i++)
            {
                Console.SetCursorPosition(1, i);
                Console.Write("\u2502");
                Console.SetCursorPosition(Console.WindowWidth - 1, i);
                Console.Write("\u2502");
            }
            Console.SetCursorPosition(1, Console.WindowHeight - 1);
            Console.Write($"\u2514{horline}\u2518");
        }

        public void DrawAuthLayout()
        { 
            DrawBoundary();
            DrawUsernameBox();
            DrawPasswordBox();
        }
    }
}
