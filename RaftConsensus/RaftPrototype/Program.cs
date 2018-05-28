using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RaftPrototype
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if (args.Length == 0) //Running the program to bootstrap
            {
                Application.Run(new RaftBootStrap());
            }
            else
            {
                string serverName = args[0];
                string configFile = args[1];
                Application.Run(new RaftNode(serverName, configFile));
            }
        }
    }
}
