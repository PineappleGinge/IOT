using Ponfig;
using System;
using System.Configuration;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace TigerBot
{
    internal class Program
    {
        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(Keys vKey);

        static void Main(string[] args)
        {
            const string NAME_VER = "Tiger Bot v1.3";
            const string SETTINGS_PATH = "Settings.txt";
            AI ai = new AI("ai.onnx");

            Ponfig.Ponfig ponfig = new Ponfig.Ponfig();
            if (File.Exists(SETTINGS_PATH))
                ponfig.Load(File.ReadAllText(SETTINGS_PATH));

            ponfig.Default("MaxDiff", "20");
            ponfig.Default("Cooldown", "1500");
            ponfig.Default("AimKey", "Z");
            ponfig.Default("TriggerKey", "OemPipe");
            ponfig.Default("EndKey", "RControlKey");
            ponfig.Default("Mirror", "True");
            ponfig.Default("XOffset", "2");
            ponfig.Default("YOffset", "2");
            ponfig.Default("SerialPort", "COM5");

            ponfig.Save(SETTINGS_PATH, true);

            int maxDiff = int.Parse(ponfig.Get("MaxDiff").Value);
            int cooldown = int.Parse(ponfig.Get("Cooldown").Value);
            Keys aimKey = (Keys) Enum.Parse(typeof(Keys), ponfig.Get("AimKey").Value, true);
            Keys triggerKey = (Keys)Enum.Parse(typeof(Keys), ponfig.Get("TriggerKey").Value, true);
            Keys endKey = (Keys) Enum.Parse(typeof(Keys), ponfig.Get("EndKey").Value, true);
            int xOffset = int.Parse(ponfig.Get("XOffset").Value);
            int yOffset = int.Parse(ponfig.Get("YOffset").Value);

            int midX = Screen.PrimaryScreen.Bounds.Width / 2;
            int midY = (Screen.PrimaryScreen.Bounds.Height / 2);

            Point left = new Point(midX - xOffset, midY + yOffset);
            Point right = new Point(midX + xOffset, midY + yOffset);

            Color startColorLeft = Color.Empty;
            Color startColorRight = Color.Empty;

            long cooldownTime = 0;
            bool init = true;

            Console.WriteLine(NAME_VER);
            Console.WriteLine($"\nMax Color Diff: {maxDiff}");
            Console.WriteLine($"Aim Key: {aimKey}");
            Console.WriteLine($"Trigger Key: {triggerKey}");
            Console.WriteLine($"Trigger Cooldown: {cooldown}");
            Console.WriteLine($"End Key: {endKey}");

            SerialPort mySerialPort = new SerialPort(ponfig.Get("SerialPort").Value);
            mySerialPort.BaudRate = 115200;
            mySerialPort.Open();

            while (true)
            {
                if (KeyPressed(endKey))
                    break;

                if (KeyPressed(aimKey))
                {
                    int[] player = ai.GetEnemy();

                    if (player == null)
                        continue;

                    mySerialPort.Write($"m{player[0]}:{player[1]}");
                }
                
                if (cooldownTime > DateTimeOffset.Now.ToUnixTimeMilliseconds())
                    continue;

                if (!KeyPressed(triggerKey))
                {
                    init = true;
                    continue;
                }

                if (init)
                {
                    startColorLeft = GetColorAt(left);
                    startColorRight = GetColorAt(right);
                    init = false;
                    continue;
                }

                if (IsSimilarColor(startColorLeft, GetColorAt(left), maxDiff) && IsSimilarColor(startColorRight, GetColorAt(right), maxDiff))
                    continue;

                mySerialPort.Write("c");
                cooldownTime = DateTimeOffset.Now.ToUnixTimeMilliseconds() + cooldown;
                
            }

            mySerialPort.Close();
            ai.Dispose();
        }

        static Color GetColorAt(Point point)
        {
            using (Bitmap pixelHolder = new Bitmap(1, 1))
            {
                using (Graphics graphics = Graphics.FromImage(pixelHolder))
                {
                    graphics.CopyFromScreen(point, Point.Empty, pixelHolder.Size);
                }
                return pixelHolder.GetPixel(0, 0);
            }
        }

        static bool IsSimilarColor(Color color1, Color color2, int maxDiff)
        {
            int redDiff = Math.Abs(color1.R - color2.R);
            int greenDiff = Math.Abs(color1.G - color2.G);
            int blueDiff = Math.Abs(color1.B - color2.B);

            return Math.Max(Math.Max(redDiff, greenDiff), blueDiff) <= maxDiff;
        }

        static bool KeyPressed(Keys vKey)
        {
            return GetAsyncKeyState(vKey) < 0;
        }
    }
}
