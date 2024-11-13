using ASCOM.Astrometry;
using ASCOM.Astrometry.AstroUtils;
using System;
using System.Collections;
using System.Device.Location;
using System.Threading;

// Things to do:
//  Mess with the dew heater. Could a duty cycle be achieved? Or connect it to a sensor?
//      Pegasus Astro Version 3 (switch).
//      Comes with its own software (API?)
//      Celestron dew heater.
//  Atmospheric sensor thing. Cloud coverage test.

// Remind carboney: bring news guy to night meeting!

namespace AscomTesting
{
    [AscomTest]
    public static class MoonPhasesTest
    {
        public static void Run()
        {
            Console.Write("This tests a few things with ASCOM computations.\n" +
                          "Requirements: No physical devices. Location discovery is recommended (but not required).\n" +
                          "Actions: Computes some things on the computer. Nothing dangerous.\n\n" +
                          "Press enter to continue > "); Console.ReadLine();

            AstroUtils utils = new AstroUtils();
            DateTime now = DateTime.Now;
            Console.WriteLine($"\nLeap seconds: {utils.LeapSeconds}");

            Console.WriteLine($"\nCurrent date is {now}");

            double julian = utils.JulianDateUtc;
            Console.WriteLine($"In julian form, that's {julian:0.000} days.");

            Console.WriteLine($"Moon phase angle: {utils.MoonPhase(julian):0.000}º");
            Console.WriteLine($"Moon illumination: {100 * utils.MoonIllumination(julian):0.000}%");

            double longitude, latitude, timeZone;
            Console.Write("\nEnter longitude of your device, or leave blank to auto-detect > ");
            string content = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(content))
            {
                Console.Write("Getting location information... ");
                GeoCoordinateWatcher locationWatcher = new GeoCoordinateWatcher();
                locationWatcher.Start();
                while (locationWatcher.Permission == GeoPositionPermission.Unknown) Thread.Sleep(100);

                if (locationWatcher.Permission == GeoPositionPermission.Denied)
                {
                    Console.WriteLine("Denied");
                    Console.Write("Enter longitude manually > ");
                    longitude = double.Parse(Console.ReadLine());
                    Console.Write("Enter latitude > ");
                    latitude = double.Parse(Console.ReadLine());
                    Console.Write("Enter time zone offset (in hours) > ");
                    timeZone = double.Parse(Console.ReadLine());
                }
                else
                {
                    while (locationWatcher.Status != GeoPositionStatus.Ready) Thread.Sleep(100);
                    GeoCoordinate loc = locationWatcher.Position.Location;
                    locationWatcher.Stop();
                    Console.WriteLine("Done");
                    longitude = loc.Longitude;
                    latitude = loc.Latitude;
                    timeZone = TimeZone.CurrentTimeZone.GetUtcOffset(now).TotalHours;

                    Console.WriteLine($"Your location is {loc.Longitude}º N, {loc.Latitude}º W");
                    Console.WriteLine($"Your time zone offset is {timeZone:0.0} hrs");
                }
            }
            else
            {
                longitude = double.Parse(content);
                Console.Write("Enter latitude > ");
                latitude = double.Parse(Console.ReadLine());
                Console.Write("Enter time zone offset (in hours) > ");
                timeZone = double.Parse(Console.ReadLine());
            }

            EventType[] types = new EventType[]
            {
                EventType.SunRiseSunset,
                EventType.MoonRiseMoonSet
            };
            foreach (EventType type in types)
            {
                Console.WriteLine($"\nTimes for {type}:");

                // Format is apparently weird.
                // list[0] is boolean, true if body is above event at midnight, false otherwise.
                // list[1] is int, represents rise events per day.
                // list[2] is int, represents set events per day.
                // list[3..(3 + rise)] is double, represents hour marks for each rise event.
                // list[(3 + rise)..] is double, represents hour marks for each set event.
                ArrayList list = utils.EventTimes(type, now.Day, now.Month, now.Year,
                                                  latitude, longitude, timeZone);
                int rises = (int)list[1];

                DateTime part = now.Date;
                for (int i = 3; i < list.Count; i++)
                {
                    double stamp = (double)list[i];
                    DateTime fullStamp = part.AddHours(stamp);
                    if (i - 3 < rises) Console.WriteLine($"Rise: {fullStamp:T}");
                    else Console.WriteLine($"Fall: {fullStamp:T}");
                }
            }

            utils.Dispose();
        }
    }
}
