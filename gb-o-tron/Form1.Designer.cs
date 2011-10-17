namespace gb_o_tron
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.panel1 = new System.Windows.Forms.Panel();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.classicGameboyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.superGameboyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.gameboyColorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.smartSelectionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // panel1
            // 
            this.panel1.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("panel1.BackgroundImage")));
            this.panel1.ContextMenuStrip = this.contextMenuStrip1;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(512, 448);
            this.panel1.TabIndex = 2;
            this.panel1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.panel1_MouseDown);
            this.panel1.MouseMove += new System.Windows.Forms.MouseEventHandler(this.panel1_MouseMove);
            this.panel1.MouseUp += new System.Windows.Forms.MouseEventHandler(this.panel1_MouseUp);
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem,
            this.classicGameboyToolStripMenuItem,
            this.superGameboyToolStripMenuItem,
            this.gameboyColorToolStripMenuItem,
            this.smartSelectionToolStripMenuItem,
            this.exitToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(165, 158);
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
            this.openToolStripMenuItem.Text = "Open ROM";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
            // 
            // classicGameboyToolStripMenuItem
            // 
            this.classicGameboyToolStripMenuItem.Name = "classicGameboyToolStripMenuItem";
            this.classicGameboyToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
            this.classicGameboyToolStripMenuItem.Text = "Classic Gameboy";
            this.classicGameboyToolStripMenuItem.Click += new System.EventHandler(this.classicGameboyToolStripMenuItem_Click);
            // 
            // superGameboyToolStripMenuItem
            // 
            this.superGameboyToolStripMenuItem.Name = "superGameboyToolStripMenuItem";
            this.superGameboyToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
            this.superGameboyToolStripMenuItem.Text = "Super Gameboy";
            this.superGameboyToolStripMenuItem.Click += new System.EventHandler(this.superGameboyToolStripMenuItem_Click);
            // 
            // gameboyColorToolStripMenuItem
            // 
            this.gameboyColorToolStripMenuItem.Name = "gameboyColorToolStripMenuItem";
            this.gameboyColorToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
            this.gameboyColorToolStripMenuItem.Text = "Gameboy Color";
            this.gameboyColorToolStripMenuItem.Click += new System.EventHandler(this.gameboyColorToolStripMenuItem_Click);
            // 
            // smartSelectionToolStripMenuItem
            // 
            this.smartSelectionToolStripMenuItem.Checked = true;
            this.smartSelectionToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.smartSelectionToolStripMenuItem.Name = "smartSelectionToolStripMenuItem";
            this.smartSelectionToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
            this.smartSelectionToolStripMenuItem.Text = "Smart Selection";
            this.smartSelectionToolStripMenuItem.Click += new System.EventHandler(this.smartSelectionToolStripMenuItem_Click);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // Form1
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(512, 448);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.Text = "GB-o-Tron";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.Form1_DragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.Form1_DragEnter);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Form1_KeyDown);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.Form1_KeyUp);
            this.contextMenuStrip1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private global::System.Windows.Forms.ToolStripMenuItem superGameboyToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem classicGameboyToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem gameboyColorToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem smartSelectionToolStripMenuItem;
    }
}

