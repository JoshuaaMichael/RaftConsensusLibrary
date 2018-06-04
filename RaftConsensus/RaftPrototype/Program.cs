using System;
using System.Windows.Forms;
using TeamDecided.RaftCommon.Logging;

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
            //Application.Run(new RaftNode("Node2", "config.json", "debug.log"));
            try
            {
                RaftLogging.Instance.EnableBuffer(500);
                if (args.Length == 0) //Running the program to bootstrap
                {
                    Application.Run(new RaftBootStrap());
                }
                else
                {
                    string serverName = args[0];
                    string configFile = args[1];
                    string logFile = args[2];
                    //Application.Run(new RaftNode(serverName, configFile, logFile));
                    Application.Run(new RaftNode(serverName, configFile, logFile));
                }
            }
            catch(Exception e)
            {
                MessageBox.Show(e.ToString());
            }
            finally
            {
                RaftLogging.Instance.FlushBuffer();
            }
        }
    }
}
