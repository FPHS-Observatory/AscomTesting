using ASCOM.DeviceInterface;
using ASCOM.DriverAccess;
using AscomTesting.Forms;
using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace AscomTesting
{
    [AscomTest]
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
            string camId = Camera.Choose("ASCOM.Simulator.Camera"); // In quotes is the default ID.
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

            while (!cam.ImageReady) Thread.Sleep(100); // Wait for the image to complete.
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
                    // Here and below, the "ref" keyword is used. This guarantees that
                    // the parameter is passed as a reference (including "structs," which
                    // are value-types and would normally be copied). For classes, it
                    // makes much less of a difference. Here, though, for reasons specified
                    // in the method, we need to resize the bitmap and we do that by creating
                    // a new one and setting the existing reference to a new value. This would
                    // not be possible without specifying "ref".
                    ReadDataColorRGGB(resX, resY, cam.ImageArray as int[,], ref result);
                    break;

                case SensorType.CMYG:
                    ReadDataColorCMYG(resX, resY, cam.ImageArray as int[,], ref result);
                    break;

                case SensorType.CMYG2:
                    ReadDataColorCMYG2(resX, resY, cam.ImageArray as int[,], ref result);
                    break;

                case SensorType.LRGB:
                    ReadDataColorLRGB(resX, resY, cam.ImageArray as int[,], ref result);
                    break;
            }
            string filename = "result.png";
            result.Save(filename);
            Console.WriteLine(" Complete");

            Console.Write("Press 'Y' to open the photo > ");
            ConsoleKeyInfo key = Console.ReadKey();
            Console.WriteLine();

            if (key.KeyChar == 'y')
            {
                // Open the image in a little viewer I made real quick.
                ImageViewer viewer = new ImageViewer(result);
                Application.Run(viewer); // Waits for the form to be closed.
            }

            cam.Dispose();
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
        public static void ReadDataColorRGGB(int width, int height, int[,] data, ref Bitmap result)
        {
            // Four indices per pixel. TL is red, TR is green, BL is green2, BR is blue.
            // Luckily the two greens are always identical, so it's not a double-size color value.
            // However, since each pixel is really a 2x2 block of data, the image is half as wide
            // and half as tall. I think, it doesn't seem like I can safely increase the X beyond
            // the width, despite that meaning that it captures less stuff.
            result.Dispose();
            result = new Bitmap(width / 2, height / 2);

            // Determine brightness ceiling.
            int maxValue = 0;
            for (int x = 0; x < width; x += 2)
            {
                for (int y = 0; y < height; y += 2)
                {
                    int rawR  = data[x    , y    ],
                        rawG1 = data[x + 1, y    ],
                        rawG2 = data[x    , y + 1],
                        rawB  = data[x + 1, y + 1];

                    if (rawR > maxValue) maxValue = rawR;
                    if (rawG1 > maxValue) maxValue = rawG1;
                    if (rawG2 > maxValue) maxValue = rawG2;
                    if (rawB > maxValue) maxValue = rawB;
                }
            }

            bool shownError = false;
            for (int x = 0; x < width; x += 2)
            {
                for (int y = 0; y < height; y += 2)
                {
                    int rawR  = data[x    , y    ],
                        rawG1 = data[x + 1, y    ],
                        rawG2 = data[x    , y + 1],
                        rawB  = data[x + 1, y + 1];

                    int rawG;

                    if (rawG1 != rawG2)
                    {
                        if (!shownError)
                        {
                            // If the two green values actually are different, I'll display a warning.
                            // The simulator doesn't have them different, but a true camera might.
                            Console.Write(" G1 != G2!");
                        }
                        // Set them to their average, since we can only have one true green value.
                        // Could be optimized with bitshifts (as could the loops), but it doesn't
                        // really matter in a debug context and it makes things clearer.
                        rawG = rawG1 / 2 + rawG2 / 2;
                    }
                    else rawG = rawG1;

                    double rf = rawR / (double)maxValue,
                           gf = rawG / (double)maxValue,
                           bf = rawB / (double)maxValue;
                    byte r = (byte)(rf * byte.MaxValue),
                         g = (byte)(gf * byte.MaxValue),
                         b = (byte)(bf * byte.MaxValue);

                    int color = (0xFF << 24) | (r << 16) | (g << 8) | b;
                    result.SetPixel(x / 2, y / 2, Color.FromArgb(color));
                }
            }
        }
        public static void ReadDataColorCMYG(int width, int height, int[,] data, ref Bitmap result)
        {
            // TODO: I'll make this later.
            //       Might take longer, because I'm not super versed in CMYK->RGB conversions.
            throw new NotImplementedException();
        }
        public static void ReadDataColorCMYG2(int width, int height, int[,] data, ref Bitmap result)
        {
            // TODO: I'll make this later.
            //       Might take longer, because I'm not super versed in CMYK->RGB conversions.
            throw new NotImplementedException();
        }
        public static void ReadDataColorLRGB(int width, int height, int[,] data, ref Bitmap result)
        {
            // TODO: I'll make this later.
            throw new NotImplementedException();
        }
    }
}
