using System;
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

            TestCameraImageData.Run();

            // You might disagree with my preference to put the readline on
            // the same line as another method call, but I think it's
            // justified because they're basically part of one whole.
            Console.WriteLine("\nPress enter to close > "); Console.ReadLine();
        }
    }
}
