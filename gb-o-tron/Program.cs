using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace gb_o_tron
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] arg)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if (arg.Length > 0)
                Application.Run(new Form1(arg[0]));
            else
                Application.Run(new Form1(""));
        }
    }
}
