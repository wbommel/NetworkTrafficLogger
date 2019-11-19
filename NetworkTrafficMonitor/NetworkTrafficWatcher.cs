using Microsoft.Win32.SafeHandles;
using NetworkTrafficMonitor.Properties;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using vobsoft.net.model;

namespace NetworkTrafficMonitor
{
    public class NetworkTrafficWatcher : IDisposable
    {
        #region declarations
        private string _fileName;
        private TrafficData _trafficData = null;
        private Machine _localMachine = null;

        private int _ioExceptionCount = 0;
        private int _exceptionCount = 0;

        #region events
        #endregion
        protected virtual void OnFileError(FileErrorEventArgs e)
        {
            EventHandler<FileErrorEventArgs> handler = FileError;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public event EventHandler<FileErrorEventArgs> FileError;
        #endregion

        #region constructor
        public NetworkTrafficWatcher()
        {
        }

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
            }

            disposed = true;
        }
        #endregion
        #endregion

        #region private functions
        private void _reloadLogfile()
        {
            bool _fileWasRead = false;
            int _timeout = 0;

            while (!_fileWasRead)
            {
                if (_timeout == 30)
                {
                    OnFileError(new FileErrorEventArgs() { Type = FileErrorEventArgs.EventType.Timeout, IOExceptionCount = _ioExceptionCount, ExceptionCount = _exceptionCount, LastMessage = "Timeout reached (30s)" });
                    break;
                }

                try
                {
                    _trafficData = null;
                    Thread.Sleep(1000);
                    _trafficData = JsonConvert.DeserializeObject<TrafficData>(File.ReadAllText(_fileName));
                    if (_trafficData.Machines.ContainsKey(Environment.MachineName)) _localMachine = _trafficData.Machines[Environment.MachineName];
                    _fileWasRead = true;
                }
                catch (IOException e)
                {
                    if (!string.IsNullOrEmpty(e.Message))
                    {
                        _ioExceptionCount++;
                        OnFileError(new FileErrorEventArgs() { Type = FileErrorEventArgs.EventType.Timeout, IOExceptionCount = _ioExceptionCount, ExceptionCount = _exceptionCount, LastMessage = e.Message });
                    }
                }
                catch (Exception ex)
                {
                    if (!string.IsNullOrEmpty(ex.Message))
                    {
                        _exceptionCount++;
                        OnFileError(new FileErrorEventArgs() { Type = FileErrorEventArgs.EventType.Timeout, IOExceptionCount = _ioExceptionCount, ExceptionCount = _exceptionCount, LastMessage = ex.Message });
                    }
                }

                _timeout++;
            }
        }

        private Reading _getNewestReading(LocalNetworkInterface lni)
        {
            long lastReadingTime = 0;
            Reading lastReading = null;
            foreach (var reading in lni.Readings.Values)
            {
                if (reading.LogTime > lastReadingTime) { lastReading = reading; }
            }

            return lastReading;
        }
        #endregion

        #region properties
        public string TestOutput
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                foreach (var ni in _localMachine.Interfaces.Values)
                {
                    Reading r = _getNewestReading(ni);
                    sb.Append(ni.Name + " - Readings: " + ni.Readings.Count + "   Bytes Received: " + r.BytesReceived + Environment.NewLine);
                }
                return sb.ToString();
            }
        }
        #endregion

        #region methods
        public void ReadTrafficData(string fileName)
        {
            _fileName = fileName;
            _reloadLogfile();
        }
        #endregion
    }



    #region event args
    public class FileErrorEventArgs
    {
        public enum EventType
        {
            Timeout,
            IOException,
            Exception
        }

        public EventType Type { get; set; }
        public int IOExceptionCount { get; set; }
        public int ExceptionCount { get; set; }
        public string LastMessage { get; set; }
    }
    #endregion
}
