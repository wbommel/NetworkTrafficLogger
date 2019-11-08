using NetworkTrafficMonitor.Properties;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using vobsoft.net.model;

namespace NetworkTrafficMonitor
{
    class Program
    {
        private static string _logFile;
        private static TrafficData _trafficData;
        private static Machine _localMachine;
        private static FileSystemWatcher _fsw;

        static void Main(string[] args)
        {
            //get logfile
            _logFile = Settings.Default.Logfile;
            if (!File.Exists(_logFile))
            {
                Console.WriteLine("Logfile not found. Exitting.");
                _anyKey();
                Console.ReadKey();
                return;
            }



            _reloadLogfile();
            if (_trafficData.Machines.ContainsKey(Environment.MachineName) && _trafficData.Machines[Environment.MachineName] is Machine)
            {
                //get machine
                _localMachine = _trafficData.Machines[Environment.MachineName];

                //initial Message
                Console.SetCursorPosition(0, 0);
                Console.WriteLine("Showing network statistics... (press x to exit)");
                _showNetworkUsage();

                //init FileSystemWatcher
                //_fsw = new FileSystemWatcher(Path.GetDirectoryName(logFile));
                //_fsw.Changed += Fsw_Changed;

                //different approach than FSW: remember time
                DateTime dtLast = DateTime.Now;

                //go to wait loop
                while (true)
                {
                    //_writeInterfaceStats();

                    if (Console.KeyAvailable)
                    {
                        ConsoleKeyInfo cki = Console.ReadKey();
                        if (cki.Key == ConsoleKey.X) { break; }
                    }

                    //check if statistics need refresh
                    if (DateTime.Now - dtLast >= TimeSpan.FromSeconds(5))
                    {
                        _reloadLogfile();
                        _showNetworkUsage();
                        dtLast = DateTime.Now;
                    }

                    Thread.Sleep(1000);
                }

                //_fsw.Changed -= Fsw_Changed;
            }



            //_anyKey();
            //Console.ReadKey();
        }

        private static void _reloadLogfile()
        {
            _trafficData = JsonConvert.DeserializeObject<TrafficData>(File.ReadAllText(_logFile));
        }

        private static void Fsw_Changed(object sender, FileSystemEventArgs e)
        {
            if (e.Name == Path.GetFileName(_logFile))
            {
                _showNetworkUsage();
            }
        }

        private static void _showNetworkUsage()
        {
            string nl = Environment.NewLine;

            Console.SetCursorPosition(0, 2);

            string strFormat = "{0,-30}{1,20}{2,20}{3,20}";
            Console.WriteLine(string.Format(strFormat, "Interface Name", "Bytes Received", "Bytes Sent", "Bytes Overall"));
            Console.WriteLine("------------------------------------------------------------------------------------------");


            foreach (var localNI in _localMachine.Interfaces.Values)
            {
                if(Settings.Default.ShowActiveAdaptersOnly && localNI.Status.ToUpper() != "UP") { continue; }

                var lastReading = localNI.Readings.Values.OrderBy(x => x.LogTime).Last();
                Console.WriteLine(string.Format(strFormat, localNI.Name, lastReading.BytesReceived, lastReading.BytesSent, lastReading.BytesReceived + lastReading.BytesSent));
            }


            Console.WriteLine(nl);
            Console.WriteLine(nl);
            Console.WriteLine(nl);
        }



        private static void _anyKey()
        {
            Console.WriteLine("Press any key.");
        }
    }
}
