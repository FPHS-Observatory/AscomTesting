using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace AscomTesting
{
    public static class Program
    {
        [STAThread] // "Single-Threaded Apartment." Required, haven't looked much into why.
        public static void Main()
        {
            // Random WinForms setup stuff. Not technically needed at
            // this stage, but would be useful if we add any UIs.
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Auto-detect all test classes. Add your own to this list by
            // adding the `[AscomTest]` attribute to the line above the class name.
            IEnumerable<Type> possible = from t in Assembly.GetExecutingAssembly().GetTypes()
                                         where t.GetCustomAttribute<AscomTestAttribute>() != null
                                         select t;
            int count = possible.Count();
            string[] names = new string[count];
            MethodInfo[] methods = new MethodInfo[count];

            int index = 0;
            foreach (Type t in possible)
            {
                names[index] = t.Name;
                methods[index] = t.GetMethod("Run", BindingFlags.Public | BindingFlags.Static);
                index++;
            }

            int selected = 0;
        _display:
            Console.SetCursorPosition(0, 0);
            Console.WriteLine("Select the test you'd like to run:");
            for (int i = 0; i < count; i++)
            {
                if (i == selected) Console.Write(" > \x1b[32m");
                else Console.Write("   ");
                Console.WriteLine(names[i] + "\x1b[0m");
            }

            ConsoleKeyInfo key = Console.ReadKey(true);
            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                    if (selected > 0) selected--;
                    goto _display;
                case ConsoleKey.DownArrow:
                    if (selected < count - 1) selected++;
                    goto _display;
                case ConsoleKey.Enter: break;
                default: goto _display;
            }

            Console.Clear();
            methods[selected].Invoke(null, null);

            // You might disagree with my preference to put the readline on
            // the same line as another method call, but I think it's
            // justified because they're basically part of one whole.
            Console.WriteLine("\nPress enter to close > "); Console.ReadLine();
        }
    }
}
