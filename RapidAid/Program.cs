using System;
using System.Windows.Forms;

namespace RapidAid
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // This line launches your Form1
            Application.Run(new Form1());
        }
    }
}