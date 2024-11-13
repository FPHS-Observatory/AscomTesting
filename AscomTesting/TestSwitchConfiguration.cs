using ASCOM.DriverAccess;
using Nerd_STF.Mathematics;
using System;
using System.Collections.Generic;
using System.Threading;

namespace AscomTesting
{
    [AscomTest]
    public static class TestSwitchConfiguration
    {
        public static void Run()
        {
            Console.Write("This tests configuring an available switch.\n" +
                          "Requirements: Switch\n" +
                          "Actions: Goes through each switch and attempts to turn it on and off.\n\n" +
                          "Press enter to continue > "); Console.ReadLine();



            string switchId = Switch.Choose("ASCOM.Simulator.Switch");
            if (string.IsNullOrWhiteSpace(switchId))
            {
                Console.WriteLine("No selection has been made! Cancelling.");
                return;
            }
            Console.WriteLine($"Chosen switch ID: \"{switchId}\"");
            Switch controller = new Switch(switchId)
            {
                Connected = true,
            };

            Console.WriteLine($"Driver Information: {controller.DriverInfo}");
            Console.WriteLine($"Description: {controller.Description}\n");
            Console.WriteLine($"Name: {controller.Name}\n");

            Console.WriteLine($"{controller.MaxSwitch} available switches.");
            for (short i = 0; i < controller.MaxSwitch; i++)
            {
                Console.WriteLine($"{controller.GetSwitchName(i)}: {controller.GetSwitch(i)} or {controller.GetSwitchValue(i)}");
            }

            Console.Write("\nPress enter to begin write test > ");
            Console.ReadLine();
            Console.WriteLine();

            Dictionary<short, int> totalMismatches = new Dictionary<short, int>();
            for (short i = 0; i < controller.MaxSwitch; i++)
            {
                if (!controller.CanWrite(i))
                {
                    Console.WriteLine($"Switch {i + 1} is not writable." + new string(' ', 10));
                    continue;
                }

                double min = controller.MinSwitchValue(i),
                       max = controller.MaxSwitchValue(i),
                       step = controller.SwitchStep(i);
                Console.WriteLine($"Switch {i + 1}... {min}/{max}/{step}" + new string(' ', 10));
                Int2 originalPos = (Console.CursorLeft, Console.CursorTop);
                double totalTime = 4;
                double iterCount = (max - min + 1) / step;
                double timePer = Math.Max(totalTime / iterCount, 0.1);
                TimeSpan delay = TimeSpan.FromSeconds(timePer);

                double original = controller.GetSwitchValue(i);
                int mismatches = 0;
                for (double v = min; v <= max; v += step)
                {
                    Console.Write($"{(mismatches > 0 ? "! " : "")}... Writing {v}");
                    Console.SetCursorPosition(originalPos.x, originalPos.y);
                    controller.SetSwitchValue(i, v);
                    Thread.Sleep(delay);
                    double compare = controller.GetSwitchValue(i);

                    if (Math.Abs(compare - v) >= step) mismatches++;
                }
                controller.SetSwitchValue(i, original);
                if (mismatches > 0) totalMismatches.Add(i, mismatches);
            }

            Console.Write("\nMismatches:");
            if (totalMismatches.Count > 0)
            {
                foreach (KeyValuePair<short, int> mismatch in totalMismatches)
                {
                    Console.WriteLine($"! Switch {mismatch.Key + 1} - {mismatch.Value} times");
                }
            }
            else Console.WriteLine(" None");
        }
    }
}
