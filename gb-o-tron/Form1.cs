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
        Bitmap border = new Bitmap(512, 448);
        Bitmap screen = new Bitmap(320, 288);
        Bitmap defaultBorder;
        private BufferedGraphics renderGfx;
        private BufferedGraphicsContext bufferContex;
        Graphics screenGfx;
        int frame;
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
        bool paused;
        int frameskip = 1;
        bool noBorder = true;

        SaveState[] saveBuffer;
        int saveBufferCounter = 0;
        int saveBufferAvaliable = 0;
        int saveBufferFreq = 2;
        int saveBufferSeconds = 240;
        bool saveSafeRewind = false;
        bool rewinding = false;
        bool rewindingEnabled = true;

        WAVOutput wavOut;
        ALAudio audio;

        public Form1(string file)
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
            screenGfx = panel1.CreateGraphics();
            appPath = Path.GetDirectoryName(Application.ExecutablePath);
            if (File.Exists(file))
                OpenFile(file);
            bufferContex = BufferedGraphicsManager.Current;
            bufferContex.MaximumBuffer = panel1.Size;
            renderGfx = bufferContex.Allocate(panel1.CreateGraphics(), new Rectangle(0, 0, panel1.Width, panel1.Height));
            renderGfx.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            System.Reflection.Assembly thisExe;
            thisExe = System.Reflection.Assembly.GetExecutingAssembly();
            Stream bitmapFile = thisExe.GetManifestResourceStream("gb_o_tron.default.png");
            defaultBorder = (Bitmap)Bitmap.FromStream(bitmapFile);
            bitmapFile.Close();
            wavOut = new WAVOutput("test.wav", 44100);
        }
        public unsafe void play()
        {
            while (!closing)
            {

                if (frameskip == 1)
                {
                    UpdateFramerate();
                    int end = DateTime.Now.Subtract(start).Milliseconds;
                    start = DateTime.Now;
                    if (end < frameRates[frameRater % 3])
                        sleep++;
                    else if (end > frameRates[frameRater % 3] && sleep != 0)
                        sleep--;
                    frameRater++;
                }
                frame++;
                Text = "GB-o-Tron - " + lastFrameRate.ToString();
                if (!paused)
                {
                    if (rewindingEnabled)
                    {
                        if (rewinding)
                        {

                            if (frame % ((saveBufferFreq == 1 ? 2 : saveBufferFreq) / 2) == 0)
                            {
                                if (saveBufferAvaliable != 0)
                                {
                                    saveBufferAvaliable--;
                                    saveBufferCounter--;
                                    if (saveBufferCounter < 0)
                                        saveBufferCounter = ((60 / saveBufferFreq) * saveBufferSeconds) - 1;

                                }
                                saveSafeRewind = true;
                            }
                            if (saveSafeRewind)
                            {
                                gb.StateLoad(saveBuffer[saveBufferCounter]);
                            }
                        }
                        else
                        {
                            saveSafeRewind = false;
                            if (frame % saveBufferFreq == 0)
                            {
                                saveBuffer[saveBufferCounter] = gb.StateSave();
                                saveBufferCounter++;
                                if (saveBufferCounter >= ((60 / saveBufferFreq) * saveBufferSeconds))
                                    saveBufferCounter = 0;
                                if (saveBufferAvaliable != ((60 / saveBufferFreq) * saveBufferSeconds))
                                    saveBufferAvaliable++;
                            }

                        }
                    }
                    gb.Run(player);
                    //wavOut.AddSamples(gb.audio.output, gb.audio.outputPTR / 2);
                    if (frameskip == 1)
                        audio.MainLoop(gb.audio.outputPTR, false);
                    if (frame % frameskip == 0)
                    {
                        BitmapData bmd = screen.LockBits(new Rectangle(0, 0, 320, 288), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                        uint* pixels = (uint*)bmd.Scan0;
                        if (gb.rom.sgbMode)
                        {
                            if (noBorder)
                                renderGfx.Graphics.DrawImage(defaultBorder, 0, 0, 512, 448);
                            for (int imgY = 0; imgY < 288; imgY++)
                                for (int imgX = 0; imgX < 320; imgX++)
                                    pixels[((imgY) * 320) + (imgX)] = gb.sgb.screen[(imgY / 2), (imgX / 2)];
                            screen.UnlockBits(bmd);
                            renderGfx.Graphics.DrawImageUnscaled(screen, new Point(96, 80));
                            if (gb.sgb.newBorder)
                            {
                                bmd = border.LockBits(new Rectangle(0, 0, 512, 448), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                                pixels = (uint*)bmd.Scan0;
                                for (int imgY = 0; imgY < 448; imgY++)
                                    for (int imgX = 0; imgX < 512; imgX++)
                                        pixels[(imgY * 512) + imgX] = gb.sgb.border[(imgY / 2), (imgX / 2)];
                                border.UnlockBits(bmd);
                                gb.sgb.newBorder = false;
                                noBorder = false;
                            }
                            if (!noBorder)
                                renderGfx.Graphics.DrawImageUnscaled(border, new Point(0, 0));
                        }
                        else
                        {
                            renderGfx.Graphics.DrawImage(defaultBorder, 0, 0, 512, 448);
                            for (int imgY = 0; imgY < 288; imgY++)
                                for (int imgX = 0; imgX < 320; imgX++)
                                    pixels[((imgY) * 320) + (imgX)] = gb.lcd.screen[(imgY / 2), (imgX / 2)];
                            screen.UnlockBits(bmd);
                            renderGfx.Graphics.DrawImageUnscaled(screen, new Point(96, 80));
                        }
                        renderGfx.Render();
                    }
                }
                if (frameskip == 1)
                    audio.SyncToAudio();
                    //Thread.Sleep(sleep);
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
            wavOut.CompleteRecording();
            if (gb != null)
            {
                closing = true;
                Thread.Sleep(50);
                if (game != null)
                    game.Abort();
                if (gb.rom.battery)
                    File.WriteAllBytes(savFile, gb.GetRam());
                if (audio != null)
                {
                    audio.Destroy();
                    audio = null;
                }
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
            if (gb != null && gb.rom.battery)
                File.WriteAllBytes(savFile, gb.GetRam());
            if (audio != null)
            {
                audio.Destroy();
                audio = null;
            }
            gb = new GBCore(File.OpenRead(file), systemType);
            noBorder = true;
            savFile = Path.Combine(appPath, "sav\\" + Path.GetFileName(file) + ".sav");
            if (gb.rom.battery && File.Exists(savFile))
                gb.SetRam(File.ReadAllBytes(savFile));
            game = new Thread(new ThreadStart(play));
            closing = false;
            saveBuffer = new SaveState[(60 / saveBufferFreq) * saveBufferSeconds];

            audio = new ALAudio(44100, gb.audio.output, 0.05f);
            audio.Create();
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
                    frameskip = 1;
                    break;
                case Keys.Oem7:
                    player.select = false;
                    break;
                case Keys.Tab:
                    rewinding = false;
                    break;
                case Keys.Space:
                    paused = !paused;
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
                    frameskip = 10;
                    break;
                case Keys.Oem7:
                    player.select = true;
                    break;
                case Keys.Tab:
                    rewinding = true;
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
        SystemType systemType = SystemType.Smart;
        private void superGameboyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            systemType = SystemType.SGB;
            superGameboyToolStripMenuItem.Checked = true;
            classicGameboyToolStripMenuItem.Checked = false;
            gameboyColorToolStripMenuItem.Checked = false;
            smartSelectionToolStripMenuItem.Checked = false;
        }

        private void classicGameboyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            systemType = SystemType.DMG;
            superGameboyToolStripMenuItem.Checked = false;
            classicGameboyToolStripMenuItem.Checked = true;
            gameboyColorToolStripMenuItem.Checked = false;
            smartSelectionToolStripMenuItem.Checked = false;

        }

        private void gameboyColorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            systemType = SystemType.GBC;
            superGameboyToolStripMenuItem.Checked = false;
            classicGameboyToolStripMenuItem.Checked = false;
            gameboyColorToolStripMenuItem.Checked = true;
            smartSelectionToolStripMenuItem.Checked = false;

        }

        private void smartSelectionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            systemType = SystemType.Smart;
            superGameboyToolStripMenuItem.Checked = false;
            classicGameboyToolStripMenuItem.Checked = false;
            gameboyColorToolStripMenuItem.Checked = false;
            smartSelectionToolStripMenuItem.Checked = true;

        }

    }
}
