using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace TigerBotV2
{
    public partial class MainForm : Form
    {
        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(Keys vKey);
        [DllImport("user32.dll", SetLastError = true)]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        // General bollocks {
        const string NameVer = "Tiger Bot v2.2";
        public enum AimModes { PredTap, Tap, Redirect, Track, Flick }
        public SerialPort SerialPort = new SerialPort("COM5", 115200);
        public Ai Ai = new Ai("Ai.onnx");
        public System.Media.SoundPlayer TigerPlayer = new System.Media.SoundPlayer(@"roar.wav");
        public bool ShouldRoar = false;
        public int MidX = Screen.PrimaryScreen.Bounds.Width / 2;
        public int MidY = (Screen.PrimaryScreen.Bounds.Height / 2);
        // }

        // Arduino-changeable settings {
        public double Sens = 1.03675;
        public int Fov = 50;
        public Keys AimKey = Keys.Z;
        public bool ToggleAim = true;
        public AimModes AimMode = AimModes.PredTap;
        public int AimCooldown = 100;
        public bool OnlyX = false;
        public bool NoPredY = true;
        public int AimSpeed = 100;
        public double SpeedMultiplier = 1;
        public bool PredSpeed = true;
        public int PredOffset = 10;

        public bool MoveCheck = true;
        public int MoveCooldown = 60;

        public Keys TriggerKey = Keys.OemPipe;
        public int MaxDiff = 20;
        public int TriggerCooldown = 1500;

        public Keys EndKey = Keys.RControlKey;

        public bool Roar = false;
        public int RoarCooldown = 5000;
        // }

        public MainForm()
        {
            InitializeComponent();
            TransparencyKey = Color.LimeGreen;
            BackColor = Color.LimeGreen;
            Location = new Point(0, 0);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            int initialStyle = GetWindowLong(Handle, -20);
            SetWindowLong(Handle, -20, initialStyle | 0x80000 | 0x20);

            SerialPort.DataReceived += new SerialDataReceivedEventHandler(SerialReceived);
            SerialPort.DtrEnable = true;
            SerialPort.Open();

            CheckForIllegalCrossThreadCalls = false;
            Thread aimbotThread = new Thread(AimbotLogic) { IsBackground = true };
            aimbotThread.Start();

            Thread triggerbotThread = new Thread(TriggerBotLogic) { IsBackground = true };
            triggerbotThread.Start();

            Thread miscThread = new Thread(MiscLogic) { IsBackground = true };
            miscThread.Start();
        }

        public void AimbotLogic()
        {
            Ai.SetMinConfidence(0.5);

            bool aimToggled = false;
            bool wasAimPressed = false;
            long moveCooldownTime = 0;
            long aimCooldownTime = 0;

            while (true)
            {
                bool moving = false;
                if (KeyPressed(Keys.W) || KeyPressed(Keys.A) || KeyPressed(Keys.S) || KeyPressed(Keys.D))
                {
                    moving = true;
                    moveCooldownTime = Mil() + MoveCooldown;
                }
                else
                    moving = false;

                if (KeyPressed(AimKey) && !wasAimPressed)
                    aimToggled = !aimToggled;
                wasAimPressed = KeyPressed(AimKey);

                if ((ToggleAim ? aimToggled : KeyPressed(AimKey))
                    && (MoveCheck ? !moving && Mil() >= moveCooldownTime : true)
                    && Mil() >= aimCooldownTime)
                {
                    aimStatusLabel.Text = @"Aim Assist : Active";

                    if (AimMode == AimModes.Tap)
                    {
                        int[] player = Ai.GetEnemy();

                        if (player == null)
                            continue;

                        if (!FovCheck(player))
                            continue;

                        int gameX = (int)Math.Round(player[0] * Sens);
                        int gameY = (int)Math.Round(player[1] * Sens * 10);

                        SerialPort.Write($"t{gameX}:{(OnlyX ? 0 : gameY)}");
                        aimCooldownTime = Mil() + AimCooldown;
                        ShouldRoar = true;
                    }
                    else if (AimMode == AimModes.Track)
                    {
                        int[] player = Ai.GetEnemy();

                        if (player == null)
                            continue;

                        if (!FovCheck(player))
                            continue;

                        int gameX = (int)Math.Round(player[0] * Sens);
                        int gameY = (int)Math.Round(player[1] * Sens * 10);

                        SerialPort.Write($"m{gameX}:{(OnlyX ? 0 : gameY)}");
                        aimCooldownTime = Mil() + AimCooldown;
                        ShouldRoar = true;
                    }
                    else if (AimMode == AimModes.PredTap)
                    {
                        long time1 = Mil();
                        int[] player1 = Ai.GetEnemy();
                        if (player1 == null)
                            continue;
                        if (!FovCheck(player1))
                            continue;

                        int time2 = (int)(Mil() - time1);
                        int[] player2 = Ai.GetEnemy();
                        if (player2 == null)
                            continue;
                        if (!FovCheck(player2))
                            continue;

                        int time3 = (int)(Mil() - time1);
                        int[] player3 = Ai.GetEnemy();
                        if (player3 == null)
                            continue;
                        if (!FovCheck(player3))
                            continue;

                        int[] finalPlayer = PointPredictor.PredictPoint(new int[] { player1[0], player1[1], 0 },
                            new int[] { player2[0], player2[1], time2 },
                            new int[] { player3[0], player3[1], time3 },
                            (int)(Mil() - time1 + 10));

                        if (!FovCheck(finalPlayer))
                            continue;

                        int gameX = (int)Math.Round(finalPlayer[0] * Sens);
                        int gameY = (int)Math.Round(finalPlayer[1] * Sens * 10);

                        SerialPort.Write($"t{gameX}:{(OnlyX ? 0 : (NoPredY ? (int)Math.Round(player3[1] * Sens * 10) : gameY))}");
                        aimCooldownTime = Mil() + AimCooldown;
                        ShouldRoar = true;
                    }
                    else if (AimMode == AimModes.Flick)
                    {
                        long time1 = Mil();
                        int[] player1 = Ai.GetEnemy();
                        if (player1 == null)
                            continue;
                        if (!FovCheck(player1))
                            continue;

                        int time2 = (int)(Mil() - time1);
                        int[] player2 = Ai.GetEnemy();
                        if (player2 == null)
                            continue;
                        if (!FovCheck(player2))
                            continue;

                        int time3 = (int)(Mil() - time1);
                        int[] player3 = Ai.GetEnemy();
                        if (player3 == null)
                            continue;
                        if (!FovCheck(player3))
                            continue;

                        int[] finalPlayer = PointPredictor.PredictPoint(new int[] { player1[0], player1[1], 500 },
                            new int[] { player2[0], player2[1], time2 },
                            new int[] { player3[0], player3[1], time3 },
                            (int)(Mil() - time1 + 10));

                        if (!FovCheck(finalPlayer))
                            continue;

                        int gameX = (int)Math.Round(finalPlayer[0] * Sens);
                        int gameY = (int)Math.Round(finalPlayer[1] * Sens * 10);

                        int steps = 2;

                        int moveX = gameX / steps;
                        int moveY = gameY / steps;

                        for (int i = 0; i < steps; i++)
                        {
                            SerialPort.Write($"m{moveX}:{(OnlyX ? 0 : (NoPredY ? (int)Math.Round(player3[1] * Sens * 10 / steps) : moveY))}");
                            Thread.Sleep(1);
                        }

                        int remX = gameX - (steps * moveX);
                        int remY = gameY - (steps * moveY);

                        SerialPort.Write($"t{remX}:{(OnlyX ? 0 : (NoPredY ? (int)Math.Round(player3[1] * Sens * 10 - (player3[1] * Sens * 10 / steps)) : remY))}");
                        aimCooldownTime = Mil() + AimCooldown;
                        ShouldRoar = true;
                    }
                    else if (AimMode == AimModes.Redirect)
                    {
                        int[] player = Ai.GetEnemy();

                        if (player == null)
                            continue;

                        if (!FovCheck(player))
                            continue;

                        int gameX = (int)Math.Round(player[0] * Sens);
                        int gameY = (int)Math.Round(player[1] * Sens * 10);

                        SerialPort.Write($"r{gameX}:{(OnlyX ? 0 : gameY)}");
                        aimCooldownTime = Mil() + AimCooldown;
                        ShouldRoar = true;
                    }
                }
                else
                    aimStatusLabel.Text = @"Aim Assist : Inactive";
            }
        }

        public void TriggerBotLogic()
        {
            Point left = new Point(MidX - 2, MidY + 2);
            Point right = new Point(MidX + 2, MidY + 2);

            Color startColorLeft = Color.Empty;
            Color startColorRight = Color.Empty;

            bool init = true;
            long triggerCooldownTime = 0;

            while (true)
            {
                triggerStatusLabel.Text = $@"Trigger Bot : {(KeyPressed(TriggerKey) ? @"Active" : @"Inactive")}";

                if (triggerCooldownTime > Mil())
                    continue;

                if (!KeyPressed(TriggerKey))
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

                if (IsSimilarColor(startColorLeft, GetColorAt(left)) && IsSimilarColor(startColorRight, GetColorAt(right)))
                    continue;

                SerialPort.Write("c");
                triggerCooldownTime = Mil() + TriggerCooldown;
                ShouldRoar = true;
            }
        }

        public void MiscLogic()
        {
            long roarCooldownTime = 0;

            while (true)
            {
                if (KeyPressed(EndKey))
                    break;

                if (ShouldRoar && Mil() < roarCooldownTime)
                {
                    ShouldRoar = false;
                    continue;
                }
                
                if (!ShouldRoar || !Roar)
                    continue;

                TigerPlayer.Play();
                roarCooldownTime = Mil() + RoarCooldown;
                ShouldRoar = false;
            }

            SerialPort.Close();
            Ai.Dispose();
            Close();
        }

        public bool KeyPressed(Keys vKey)
        {
            return GetAsyncKeyState(vKey) < 0;
        }

        public Color GetColorAt(Point point)
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

        public bool IsSimilarColor(Color color1, Color color2)
        {
            int redDiff = Math.Abs(color1.R - color2.R);
            int greenDiff = Math.Abs(color1.G - color2.G);
            int blueDiff = Math.Abs(color1.B - color2.B);

            return Math.Max(Math.Max(redDiff, greenDiff), blueDiff) <= MaxDiff;
        }

        public bool FovCheck(int[] player)
        {
            if (player[2] <= Fov)
                Console.WriteLine($"{player[2]} | Fov");
            return player[2] <= Fov;
        }

        public long Mil()
        {
            return DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }

        public void SerialReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string[] parms = SerialPort.ReadLine().Split(':');
            string name = parms[0];
            string value = parms[1];

            switch (name)
            {
                case "Sensitivity":
                    Sens = 1.03675 / double.Parse(value);
                    break;
                case "Confidence":
                    Ai.SetMinConfidence(double.Parse(value));
                    break;
                case "FOV":
                    Fov = int.Parse(value);
                    Invalidate();
                    break;
                case "Aim Key":
                    AimKey = (Keys)Enum.Parse(typeof(Keys), value);
                    break;
                case "Toggle Aim":
                    ToggleAim = bool.Parse(value);
                    break;
                case "Aim Mode":
                    AimMode = (AimModes)Enum.Parse(typeof(AimModes), value);
                    break;
                case "Aim Cooldown":
                    AimCooldown = int.Parse(value);
                    break;
                case "X Only":
                    OnlyX = bool.Parse(value);
                    break;
                case "No Pred Y":
                    NoPredY = bool.Parse(value);
                    break;
                case "Move Check":
                    MoveCheck = bool.Parse(value);
                    break;
                case "Move Cooldown":
                    MoveCooldown = int.Parse(value);
                    break;
                case "Trigger Key":
                    TriggerKey = (Keys)Enum.Parse(typeof(Keys), value);
                    break;
                case "Trigger Diff":
                    MaxDiff = int.Parse(value);
                    break;
                case "Trigger Cooldown":
                    TriggerCooldown = int.Parse(value);
                    break;
                case "End Key":
                    EndKey = (Keys)Enum.Parse(typeof(Keys), value);
                    break;
                case "Roar":
                    Roar = bool.Parse(value);
                    break;
                case "Roar Cooldown":
                    RoarCooldown = int.Parse(value);
                    break;
            }
        }

        private void MainForm_Paint(object sender, PaintEventArgs e)
        {
            int x = ClientRectangle.Width / 2;
            int y = ClientRectangle.Height / 2;
            int radius = Fov / 2;

            Pen pen = new Pen(Color.FromArgb(150, 255, 255, 255), 1);

            e.Graphics.DrawEllipse(pen, new Rectangle(x - radius, y - radius, Fov, Fov));
        }
    }
}
