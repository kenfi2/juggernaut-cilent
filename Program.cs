using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace juggernaut_client
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        private static BaseForm m_mainForm = null;
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            m_mainForm = new LoginWindow();
            Application.Run(m_mainForm);
        }

        public static BaseForm MainForm { get => m_mainForm; set => m_mainForm = value; }
    }
}
