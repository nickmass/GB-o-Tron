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
        Bitmap screen = new Bitmap(512, 448);
        Graphics screenGfx;
        int frames;
        int lastFrameRate;
        int[] frameRates = { 16, 17, 17};
        int frameRater;
        int lastTickCount;
        int sleep;
        Input player;
        DateTime start;
        string appPath;
        string savFile;

        public Form1(string file)
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
            screenGfx = panel1.CreateGraphics();
            appPath = Path.GetDirectoryName(Application.ExecutablePath);
            if (File.Exists(file))
                OpenFile(file);
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
                Text = "GB-o-Tron - " + lastFrameRate.ToString();
                gb.Run(player);
                BitmapData bmd = screen.LockBits(new Rectangle(0, 0, 512, 448), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                uint* pixels = (uint*)bmd.Scan0;
                if (gb.rom.SGB)
                {
                    for (int imgY = 0; imgY < 288; imgY++)
                        for (int imgX = 0; imgX < 320; imgX++)
                            pixels[((imgY + 80) * 512) + (imgX + 96)] = gb.sgb.screen[(imgY / 2), (imgX / 2)];
                    screen.UnlockBits(bmd);
                    screenGfx.DrawImage(screen, new Point(0, 0));
                    if (gb.sgb.newBorder)
                    {
                        bmd = screen.LockBits(new Rectangle(0, 0, 512, 448), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                        pixels = (uint*)bmd.Scan0;
                        for (int imgY = 0; imgY < 448; imgY++)
                            for (int imgX = 0; imgX < 512; imgX++)
                                pixels[(imgY * 512) + imgX] = gb.sgb.border[(imgY / 2), (imgX / 2)];
                        screen.UnlockBits(bmd);
                        screenGfx.DrawImage(screen, new Point(0, 0));
                        gb.sgb.newBorder = false;
                    }
                }
                else
                {
                    for (int imgY = 0; imgY < 288; imgY++)
                        for (int imgX = 0; imgX < 320; imgX++)
                            pixels[((imgY + 80) * 512) + (imgX + 96)] = gb.lcd.screen[(imgY / 2), (imgX / 2)];
                    screen.UnlockBits(bmd);
                    screenGfx.DrawImage(screen, new Point(0, 0));
                }
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
                closing = true;
                Thread.Sleep(50);
                if (game != null)
                    game.Abort();
                if (gb.rom.battery)
                    File.WriteAllBytes(savFile, gb.GetRam());
            }
        }
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                OpenFile(openFileDialog1.FileName);
            }

        }
        private void OpenFile(string file)
        {
            closing = true;
            Thread.Sleep(50);
            if (game != null)
                game.Abort();
            gb = new GBCore(File.OpenRead(file), superGameboyToolStripMenuItem.Checked ? SystemType.SGB : SystemType.GBC);
            savFile = Path.Combine(appPath, "sav\\" + Path.GetFileName(file) + ".sav");
            if (gb.rom.battery && File.Exists(savFile))
                gb.SetRam(File.ReadAllBytes(savFile));
            game = new Thread(new ThreadStart(play));
            closing = false;
            game.Start();
        }
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
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
        Point startPoint;
        bool drag;
        private void panel1_MouseMove(object sender, MouseEventArgs e)
        {
            if (drag)
            {
                Point p1 = new Point(e.X, e.Y);
                Point p2 = PointToScreen(p1);
                Point p3 = new Point(p2.X - startPoint.X, p2.Y - startPoint.Y);
                Location = p3;
            }
        }

        private void panel1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                drag = true;
                startPoint = new Point(e.X, e.Y);
            }
        }

        private void panel1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                drag = false;
            }
        }
        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            OpenFile(((string[])e.Data.GetData(DataFormats.FileDrop))[0]);
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, false) == true)
                e.Effect = DragDropEffects.All;
        }

    }
}
