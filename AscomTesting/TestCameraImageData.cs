using ASCOM.DeviceInterface;
using ASCOM.DriverAccess;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;

namespace AscomTesting
{
    public static class TestCameraImageData
    {
        public static void Run()
        {
            // Probably good practice to make clear what we're about to do.
            Console.Write("This tests how a camera's image is formatted.\n" +
                          "Requirements: Camera\n" +
                          "Actions: Expose one photo for a given amount of time, then attempt to write its image data to a file.\n\n" +
                          "Press enter to continue > "); Console.ReadLine();



            // Select camera from the chooser. This morning I was
            // using the general chooser, but we can also do this.
            string camId = Camera.Choose(""); // In quotes is the default ID.
            if (string.IsNullOrWhiteSpace(camId))
            {
                Console.WriteLine("No selection has been made! Cancelling.");
                return;
            }

            // What you see done in the print statement is called "string interpolation."
            // You can look it up if you want, but it is just a neater way of concatenating
            // content in a string. It's determined by putting a dollar sign before the quotes,
            // and then anything inside curly braces is code. There's some formatting stuff but
            // it's a bit much to explain in a comment. To include a curly brace as a piece of
            // *text* in an interpolated string, use '{{'.
            Console.WriteLine($"Chosen camera ID: \"{camId}\"");
            Camera cam = new Camera(camId)
            {
                Connected = true
            };
            // What you see above is a property assignment done as part of a constructor.
            // It's equivalent to doing `cam.Connected = true;` directly after the constructor.
            // Makes it more clear when setting multiple properties, but here it's a little
            // silly. Doing it because of VS suggestion.
            Console.WriteLine($"Driver Information: {cam.DriverInfo}");
            Console.WriteLine($"Description: {cam.Description}");

            Console.Write("\nEnter the time to expose for in seconds (blank is 1s) > ");
            string exposureStr = Console.ReadLine();
            double exposure = string.IsNullOrWhiteSpace(exposureStr) ? 1 : double.Parse(exposureStr);

            Console.Write("Starting exposure...");
            cam.StartExposure(exposure, false);
            Thread.Sleep((int)(exposure * 1100) + 500);
            // The sleep call gives some extra time. 110% * time + 0.5s.
            // TODO: The properties "ImageReady" and "CameraState" might
            //       be able to improve this beyond just a simple waiting
            //       script, but I won't use that for this test.
            Console.WriteLine(" Complete");

            int resX = cam.CameraXSize, resY = cam.CameraYSize;
            Console.WriteLine($"\nCamera Resolution: ({resX}, {resY})");

            // No simplified constructor in versions of c# below 9.0.
            // This means versions of .NET below 6.0 core, and all
            // versions of .NET framework (they all use c# 7.3).
            Bitmap result = new Bitmap(resX, resY);
            Console.WriteLine($"Image data is of the form {cam.ImageArray.GetType()}");
            Console.WriteLine($"Sensor Type: {cam.SensorType}");

            // We pass the result as a reference instead of returning a new one here.
            // This could work both ways, but just to demonstrate an alternative that
            // is used in cases where it does maks a different, we'll do it here.
            Console.Write("Writing file...");
            switch (cam.SensorType)
            {
                case SensorType.Monochrome:
                    // The "as" keyword here is a type of cast.
                    ReadDataMonochrome(resX, resY, cam.ImageArray as int[,], result);
                    break;

                case SensorType.Color:
                    // This is the only case where the data is a 3D array.
                    ReadDataColor(resX, resY, cam.ImageArray as int[,,], result);
                    break;

                case SensorType.RGGB:
                    ReadDataColorRGGB(resX, resY, cam.ImageArray as int[,], result);
                    break;

                case SensorType.CMYG:
                    ReadDataColorCMYG(resX, resY, cam.ImageArray as int[,], result);
                    break;

                case SensorType.CMYG2:
                    ReadDataColorCMYG2(resX, resY, cam.ImageArray as int[,], result);
                    break;

                case SensorType.LRGB:
                    ReadDataColorLRGB(resX, resY, cam.ImageArray as int[,], result);
                    break;
            }
            string filename = "result.bmp";
            result.Save(filename);
            Console.WriteLine(" Complete");

            Console.Write("Press 'Y' to open the photo > ");
            ConsoleKeyInfo key = Console.ReadKey();
            Console.WriteLine();

            if (key.KeyChar == 'y')
            {
                // Open the file. Find the location of the program and then use that to open the image.
                // The '@' here tells the string to not determine any escape codes. Makes things easier
                // for file paths. Still works with interpolation if you specify the '$'.
                string location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string fileLocation = location + $@"\{filename}";
                Console.WriteLine($"Opening image at {fileLocation}");
                Process proc = Process.Start(fileLocation);
                proc.WaitForExit();
            }
        }

        // All of these use dynamic scaling because the values we get are not clamped to
        // a maximum. They are dependent on the electron count for each channel, and as
        // such, a longer exposure means higher values all around.
        public static void ReadDataMonochrome(int width, int height, int[,] data, Bitmap result)
        {
            // One index per pixel. Contains the number of grayscale electrons.

            // Determine brightness ceiling.
            int maxValue = 0; // Changing the default here specifies the minimum brightness ceiling.
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int rawK = data[x, y];
                    if (rawK > maxValue) maxValue = rawK;
                }
            }

            // Actually set bitmap pixels.
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int rawK = data[x, y];
                    double kf = rawK / (double)maxValue;
                    byte k = (byte)(kf * byte.MaxValue);

                    int color = (0xFF << 24) | (k << 16) | (k << 8) | k;
                    result.SetPixel(x, y, Color.FromArgb(color));
                }
            }
        }
        public static void ReadDataColor(int width, int height, int[,,] data, Bitmap result)
        {
            // Three indices per pixel, however instead of being sequential in a 2D array, they
            // are sequential in a new third dimension, representing the channel index. [x, y, channel]

            // Determine brightness ceiling.
            int maxValue = 0;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int rawR = data[x, y, 0],
                        rawG = data[x, y, 1],
                        rawB = data[x, y, 2];
                    if (rawR > maxValue) maxValue = rawR;
                    if (rawG > maxValue) maxValue = rawG;
                    if (rawB > maxValue) maxValue = rawB;
                }
            }

            // Actually set bitmap pixels.
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int rawR = data[x, y, 0],
                        rawG = data[x, y, 1],
                        rawB = data[x, y, 2];
                    double rf = rawR / (double)maxValue,
                           gf = rawG / (double)maxValue,
                           bf = rawB / (double)maxValue;
                    byte r = (byte)(rf * byte.MaxValue),
                         g = (byte)(gf * byte.MaxValue),
                         b = (byte)(bf * byte.MaxValue);

                    int color = (0xFF << 24) | (r << 16) | (g << 8) | b;
                    result.SetPixel(x, y, Color.FromArgb(color));
                }
            }
        }
        public static void ReadDataColorRGGB(int width, int height, int[,] data, Bitmap result)
        {
            // TODO: I'll make this later.
            throw new NotImplementedException();
        }
        public static void ReadDataColorCMYG(int width, int height, int[,] data, Bitmap result)
        {
            // TODO: I'll make this later.
            //       Might take longer, because I'm not super versed in CMYK->RGB conversions.
            throw new NotImplementedException();
        }
        public static void ReadDataColorCMYG2(int width, int height, int[,] data, Bitmap result)
        {
            // TODO: I'll make this later.
            //       Might take longer, because I'm not super versed in CMYK->RGB conversions.
            throw new NotImplementedException();
        }
        public static void ReadDataColorLRGB(int width, int height, int[,] data, Bitmap result)
        {
            // TODO: I'll make this later.
            throw new NotImplementedException();
        }
    }
}
