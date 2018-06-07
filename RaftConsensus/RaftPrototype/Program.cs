using System;
using System.IO;
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
            try
            {
                RaftLogging.Instance.EnableBuffer(50);
                if (args.Length == 0) //Running the program to bootstrap
                {
                    Application.Run(new RaftBootStrap());
                }
                else
                {
                    string serverName = args[0];
                    string configFile = args[1];
                    string logFile = args[2];
                    Application.Run(new RaftNode(serverName, configFile, logFile));
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                RaftLogging.Instance.FlushBuffer();
            }
        }
    }
}