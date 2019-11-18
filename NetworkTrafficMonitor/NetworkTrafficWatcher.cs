using Microsoft.Win32.SafeHandles;
using NetworkTrafficMonitor.Properties;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using vobsoft.net.model;

namespace NetworkTrafficMonitor
{
    public class NetworkTrafficWatcher : IDisposable
    {
        #region declarations

        #endregion

        #region constructor

        #region dispose pattern accoring to MSDN article (https://docs.microsoft.com/de-de/dotnet/standard/garbage-collection/implementing-dispose)
        // Flag: Has Dispose already been called?
        bool disposed = false;
        // Instantiate a SafeHandle instance.
        SafeHandle handle = new SafeFileHandle(IntPtr.Zero, true);

        // Public implementation of Dispose pattern callable by consumers.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                handle.Dispose();
                // Free any other managed objects here.
                //
                _trafficData = null;
                _localMachine = null;
                _fsw = null;
            }

            disposed = true;
        }
        #endregion
        #endregion

        #region private functions
        #endregion

        #region properties
        #endregion

        #region methods
        #endregion
    }



    public class NetworkTrafficWatcherOld : IDisposable
    {
        #region declarations
        private string _logFile;
        private TrafficData _trafficData;
        private Machine _localMachine;
        private FileSystemWatcher _fsw;

        private int _exceptionCount = 0;
        private int _ioExceptionCount = 0;

        private bool _breakLoop = false;
        #endregion

        #region constructor

        #region dispose pattern accoring to MSDN article (https://docs.microsoft.com/de-de/dotnet/standard/garbage-collection/implementing-dispose)
        // Flag: Has Dispose already been called?
        bool disposed = false;
        // Instantiate a SafeHandle instance.
        SafeHandle handle = new SafeFileHandle(IntPtr.Zero, true);

        // Public implementation of Dispose pattern callable by consumers.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                handle.Dispose();
                // Free any other managed objects here.
                //
                _trafficData = null;
                _localMachine = null;
                _fsw = null;
            }

            disposed = true;
        }
        #endregion
        #endregion

        #region private functions
        private void _reloadLogfile()
        {
            bool _fileWasRead = false;
            while (!_fileWasRead)
            {
                try
                {
                    _trafficData = null;
                    Thread.Sleep(1000);
                    _trafficData = JsonConvert.DeserializeObject<TrafficData>(File.ReadAllText(_logFile));
                    _fileWasRead = true;
                }
                catch (IOException e)
                {
                    if (!string.IsNullOrEmpty(e.Message))
                    {
                        _ioExceptionCount++;
                        Console.WriteLine("IOExceptions: " + _ioExceptionCount + "    Exceptions: " + _exceptionCount);
                    }
                }
                catch (Exception ex)
                {
                    if (!string.IsNullOrEmpty(ex.Message))
                    {
                        _exceptionCount++;
                        Console.WriteLine("IOExceptions: " + _ioExceptionCount + "    Exceptions: " + _exceptionCount);
                    }
                }
            }
        }

        private void Fsw_Changed(object sender, FileSystemEventArgs e)
        {
            if (e.Name == Path.GetFileName(_logFile))
            {
                _reloadLogfile();
                _showNetworkUsage();
            }
        }

        private void _showNetworkUsage()
        {
            string nl = Environment.NewLine;

            Console.SetCursorPosition(0, 2);

            string strFormat = "{0,-30}{1,20}{2,20}{3,20}";
            Console.WriteLine(string.Format(strFormat, "Interface Name", "Bytes Received", "Bytes Sent", "Bytes Overall"));
            Console.WriteLine("------------------------------------------------------------------------------------------");


            foreach (var localNI in _localMachine.Interfaces.Values)
            {
                if (Settings.Default.ShowActiveAdaptersOnly && localNI.Status.ToUpper() != "UP") { continue; }

                //var lastReading = localNI.Readings.Values.OrderBy(x => x.LogTime).Last();
                var lastReading = new Reading() { LogTime = 0 };
                foreach (var r in localNI.Readings.Values)
                {
                    if (r.LogTime > lastReading.LogTime)
                    {
                        lastReading = r;
                    }
                }
                Console.WriteLine(string.Format(strFormat, localNI.Name, lastReading.BytesReceived, lastReading.BytesSent, lastReading.BytesReceived + lastReading.BytesSent));
            }


            Console.WriteLine(nl);
            Console.WriteLine(nl);
            Console.WriteLine(nl);
        }

        private void _watchIt()
        {
            //get logfile
            _logFile = Settings.Default.Logfile;

#if DEBUG
            _logFile = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\..\..\..\NetworkAdapterTest\bin\Debug\NetworkTraffic.json";
#endif
            if (!File.Exists(_logFile))
            {
                throw new FileNotFoundException("Logfile not found: " + _logFile);
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

                //use FileSystemWatcher
                using (_fsw = new FileSystemWatcher(Path.GetDirectoryName(_logFile)))
                {
                    _fsw.Changed += Fsw_Changed;
                    _fsw.EnableRaisingEvents = true;

                    //go to wait loop
                    while (true)
                    {
                        if (Console.KeyAvailable)
                        {
                            ConsoleKeyInfo cki = Console.ReadKey();
                            if (cki.Key == ConsoleKey.X) { break; }
                        }

                        if (_breakLoop) { break; }

                        _reloadLogfile();
                        _showNetworkUsage();

                        Thread.Sleep(1000);
                    }

                    _fsw.EnableRaisingEvents = false;
                    _fsw.Changed -= Fsw_Changed;
                }
            }
        }
        #endregion

        #region properties
        #endregion

        #region methods
        public void StartWatching()
        {
            _watchIt();
        }

        public void StopWatching()
        {
            _breakLoop = true;
        }
        #endregion
    }
}
