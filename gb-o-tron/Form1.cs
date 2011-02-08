using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Threading;

namespace gb_o_tron
{
    public partial class Form1 : Form
    {
        bool closing;
        GBCore gb;
        Thread game;
        Bitmap screen = new Bitmap(320,288);
        Graphics screenGfx;
        int frames;
        int lastFrameRate;
        int[] frameRates = { 16, 17, 17};
        int frameRater;
        int lastTickCount;
        int sleep;
        Input player;
        DateTime start;
        public Form1()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
            screenGfx = panel1.CreateGraphics();
        }
        public unsafe void play()
        {
            while (!closing)
            {
                UpdateFramerate();
                int end = DateTime.Now.Subtract(start).Milliseconds;
                start = DateTime.Now;
                if (end < frameRates[frameRater % 3])
                    sleep++;
                else if (end > frameRates[frameRater % 3] && sleep != 0)
                    sleep--;
                frameRater++;
                this.Text = lastFrameRate.ToString();
                gb.Run(player);
                BitmapData bmd = screen.LockBits(new Rectangle(0, 0, 320, 288), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                uint* pixels = (uint*)bmd.Scan0;
                for (int imgY = 0; imgY < 288; imgY++)
                    for (int imgX = 0; imgX < 320; imgX++)
                        pixels[(imgY * 320) + imgX] = gb.lcd.screen[(imgY / 2), (imgX / 2)];
                //for (int imgY = 0; imgY < 144; imgY++)
                //    for (int imgX = 0; imgX < 160; imgX++)
                //        pixels[(imgY * 320) + imgX] = gb.lcd.screen[(imgY), (imgX)];
                screen.UnlockBits(bmd);
                screenGfx.DrawImage(screen, new Point(0,0));
                Thread.Sleep(sleep);
            }
        }
        private void UpdateFramerate()
        {
            frames++;
            if (Math.Abs(Environment.TickCount - lastTickCount) > 1000)
            {
                lastFrameRate = frames * 1000 / Math.Abs(Environment.TickCount - lastTickCount);
                lastTickCount = Environment.TickCount;
                frames = 0;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (gb != null)
            {
                game.Abort();
                closing = true;
                Thread.Sleep(50);
                if (gb.rom.battery)
                    File.WriteAllBytes(openFileDialog1.SafeFileName + ".sav", gb.GetRam());
            }
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Up:
                    player.up = false;
                    break;
                case Keys.Down:
                    player.down = false;
                    break;
                case Keys.Left:
                    player.left = false;
                    break;
                case Keys.Right:
                    player.right = false;
                    break;
                case Keys.Z:
                    player.a = false;
                    break;
                case Keys.X:
                    player.b = false;
                    break;
                case Keys.Enter:
                    player.start = false;
                    break;
                case Keys.ShiftKey:
                    player.select = false;
                    break;
            }

        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Up:
                    player.up = true;
                    break;
                case Keys.Down:
                    player.down = true;
                    break;
                case Keys.Left:
                    player.left = true;
                    break;
                case Keys.Right:
                    player.right = true;
                    break;
                case Keys.Z:
                    player.a = true;
                    break;
                case Keys.X:
                    player.b = true;
                    break;
                case Keys.Enter:
                    player.start = true;
                    break;
                case Keys.ShiftKey:
                    player.select = true;
                    break;
            }
        }

        private void panel1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                if (game != null)
                    game.Abort();
                closing = true;
                if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    Thread.Sleep(50);
                    gb = new GBCore(File.OpenRead(openFileDialog1.FileName));
                    if (gb.rom.battery && File.Exists(openFileDialog1.SafeFileName + ".sav"))
                        gb.SetRam(File.ReadAllBytes(openFileDialog1.SafeFileName + ".sav"));
                    game = new Thread(new ThreadStart(play));
                    closing = false;
                    game.Start();
                }
            }
        }
    }
}
