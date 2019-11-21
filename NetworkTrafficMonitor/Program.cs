using NetworkTrafficMonitor.Properties;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using vobsoft.net;

namespace NetworkTrafficMonitor
{
    class Program
    {
        private static string _logFile;
        private static FileSystemWatcher _fsw;

        static void Main(string[] args)
        {
            //get logfile
            _logFile = Settings.Default.Logfile;

#if DEBUG
            _logFile = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\..\..\..\NetworkAdapterTest\bin\Debug\NetworkTraffic.json";
#endif
            if (!File.Exists(_logFile))
            {
                Console.WriteLine("Logfile not found. Exitting.");
                _anyKey();
                Console.ReadKey();
                return;
            }

            //initial Message
            //_initialMessage();
            _outputUsage();

            //init FileSystemWatcher
            _fsw = new FileSystemWatcher(Path.GetDirectoryName(_logFile));
            _fsw.Changed += Fsw_Changed;
            _fsw.EnableRaisingEvents = true;

            //go to wait loop
            while (true)
            {
                //_writeInterfaceStats();

                if (Console.KeyAvailable)
                {
                    ConsoleKeyInfo cki = Console.ReadKey();
                    if (cki.Key == ConsoleKey.X) { break; }
                }

                Thread.Sleep(1000);
            }
        }

        private static void _initialMessage()
        {
            Console.SetCursorPosition(0, 0);
            Console.WriteLine("Showing network statistics... (press x to exit)");
        }

        private static void Fsw_Changed(object sender, FileSystemEventArgs e)
        {
            if (e.Name == Path.GetFileName(_logFile))
            {
                _outputUsage();
            }
        }

        private static void _outputUsage()
        {
            using (var ntw = new NetworkTrafficWatcher())
            {
                ntw.ReadTrafficData(_logFile);
                Console.Clear();
                _initialMessage();
                Console.SetCursorPosition(0, 2);
                Console.WriteLine(ntw.TestOutput);
            }
        }

        private static void _anyKey()
        {
            Console.WriteLine("Press any key.");
        }
    }
}
