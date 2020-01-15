using LiteDB;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using vobsoft.net.LiteDBLogger;
using vobsoft.net.LiteDBLogger.model;

namespace vobsoft.net
{
    public class NetworkTrafficWatcherModelLiteDB : IDisposable
    {
        #region declarations
        private string _fileName;

        private Machine _localMachine;

        private LiteDatabase _db;
        LiteCollection<Machine> _allMachines;
        LiteCollection<LocalNetworkInterface> _allInterfaces;
        IEnumerable<LocalNetworkInterface> _localInterfaces;
        LiteCollection<Reading> _allRreadings;

        private int _ioExceptionCount = 0;
        private int _exceptionCount = 0;

        #region events
        protected virtual void OnFileError(FileErrorEventArgs e)
        {
            FileError?.Invoke(this, e);
        }

        public event EventHandler<FileErrorEventArgs> FileError;
        #endregion
        #endregion

        #region constructor
        public NetworkTrafficWatcherModelLiteDB(string fileName)
        {
            _fileName = fileName;
            if (!File.Exists(_fileName))
            {
                OnFileError(new FileErrorEventArgs()
                {
                    ExceptionCount = 1,
                    IOExceptionCount = 0,
                    Type = FileErrorEventArgs.EventType.FileNotFound,
                    LastMessage = "File not found: " + _fileName
                });
            }

            _db = new LiteDatabase(new ConnectionString()
            {
                Filename = _fileName,
                Mode = ConnectionMode.Shared
            });

            //fetch data
            _allMachines = _db.GetCollection<Machine>(Constants.COLLECTION_MACHINES);
            _allInterfaces = _db.GetCollection<LocalNetworkInterface>(Constants.COLLECTION_INTERFACES);
            _allRreadings = _db.GetCollection<Reading>(Constants.COLLECTION_READINGS);

            _fetchLocalMachine();
            _fetchLocalInterfaces();

            //create indexes
            _allInterfaces.EnsureIndex(x => x.MachineId);
            _allRreadings.EnsureIndex(x => x.InterfaceId);
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
                _localMachine = null;
                _allMachines = null;
                _allInterfaces = null;
                _allRreadings = null;
                _db.Dispose();
            }

            disposed = true;
        }
        #endregion
        #endregion

        #region private functions
        private void _fetchLocalMachine()
        {
            _localMachine = _allMachines.FindOne(x => x.MachineName == Environment.MachineName);
            if (_localMachine == null) { }
        }

        private void _fetchLocalInterfaces()
        {
            _localInterfaces = _allInterfaces.Find(Query.EQ("MachineId", _localMachine.Id));
        }

        //private Reading _getNewestReading(LocalNetworkInterface lni)
        //{
        //    long lastReadingTime = 0;
        //    Reading lastReading = null;
        //    foreach (var reading in lni.Readings.Values)
        //    {
        //        if (reading.LogTime > lastReadingTime) { lastReading = reading; }
        //    }

        //    return lastReading;
        //}
        private Reading _getNewestReadingForInterface(LocalNetworkInterface lni)
        {
            _allRreadings.EnsureIndex(x => x.InterfaceId);

            System.Linq.Enumerable results = _allRreadings.Find(x => x.InterfaceId == lni.Id);
            

            return null;
        }

        //private Dictionary<long, DayReading> _getDailyUsage(LocalNetworkInterface lni)
        //{
        //    var retVal = new Dictionary<long, DayReading>();
        //    long zwischenReceived = 0;
        //    long zwischenSent = 0;
        //    long day;
        //    //var lastReading = new DayReading() { Day = 0, BytesReceived = 0, BytesSent = 0 };

        //    foreach (var reading in lni.Readings.Values)
        //    {
        //        //get day information and see if lastReading is still ok
        //        day = long.Parse(reading.LogTime.ToString().Substring(0, 8));

        //        if (!retVal.ContainsKey(day))
        //        {
        //            retVal.Add(day, new DayReading() { Day = day, BytesReceived = reading.BytesReceived, BytesSent = reading.BytesSent });
        //            zwischenReceived = 0;
        //            zwischenSent = 0;
        //        }
        //        else
        //        {
        //            //potentially set zwischen values
        //            if (reading.BytesReceived + zwischenReceived < retVal[day].BytesReceived)
        //            {
        //                zwischenReceived += retVal[day].BytesReceived;
        //                zwischenSent += retVal[day].BytesSent;
        //            }

        //            retVal[day].BytesReceived = reading.BytesReceived + zwischenReceived;
        //            retVal[day].BytesSent = reading.BytesSent + zwischenSent;
        //        }
        //    }

        //    return retVal;
        //}
        #endregion

        #region properties
        public LiteCollection<LocalNetworkInterface> LocalInterfaces { get { return _allInterfaces; } }

        public string TestOutput
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                
                foreach (var ni in _localInterfaces)
                {
                    sb.Append("Id: " + ni.Id + Environment.NewLine);
                    sb.Append("MachineId: " + ni.MachineId + Environment.NewLine);
                    sb.Append("Name: " + ni.Name + Environment.NewLine);
                    sb.Append("Description: " + ni.Description + Environment.NewLine);
                    sb.Append("InterfaceGUID: " + ni.InterfaceGUID + Environment.NewLine);
                    sb.Append("Type: " + ni.Type + Environment.NewLine);
                    sb.Append("Status: " + ni.Status + Environment.NewLine);
                    sb.Append("Speed: " + ni.Speed + Environment.NewLine);
                }
                return sb.ToString();
            }
        }

        //public string GetDailyUsagesOfInterface(string interfaceId)
        //{
        //    //prepare vars
        //    var result = string.Empty;

        //    //read data
        //    _reloadLogfile();

        //    foreach (var ni in _localMachine.Interfaces.Values)
        //    {
        //        //early continue
        //        if (ni.InterfaceId != interfaceId) { continue; }

        //        //get data
        //        StringBuilder sb = new StringBuilder();
        //        var dayReadings = _getDailyUsage(ni);
        //        foreach (var dr in dayReadings.Values)
        //        {
        //            long usage = dr.BytesReceived + dr.BytesSent;
        //            sb.Append("     " + dr.Day + ": " + usage.ToString("N0") + Environment.NewLine);
        //        }

        //        result = sb.ToString();

        //        break;
        //    }

        //    return result;
        //}

        //public long GetTodaysUsageOfInterface(string interfaceId)
        //{
        //    //prepare vars
        //    var result = (long)0;
        //    var today = long.Parse(DateTime.Now.Year.ToString() + DateTime.Now.Month.ToString("00") + DateTime.Now.Day.ToString("00"));

        //    //read data
        //    _reloadLogfile();

        //    foreach (var ni in _localMachine.Interfaces.Values)
        //    {
        //        //early continue
        //        if (ni.InterfaceId != interfaceId) { continue; }

        //        //get data
        //        var dayReadings = _getDailyUsage(ni);
        //        foreach (var dr in dayReadings.Values)
        //        {
        //            //early continue
        //            if (dr.Day != today) { continue; }

        //            result = dr.BytesReceived + dr.BytesSent;

        //            break;
        //        }

        //        break;
        //    }

        //    return result;
        //}

        //public long GetUsageOfInterfaceSince(string interfaceId, long usageSinceDay)
        //{
        //    //prepare vars
        //    var result = (long)0;
        //    var today = long.Parse(DateTime.Now.Year.ToString() + DateTime.Now.Month.ToString("00") + DateTime.Now.Day.ToString("00"));
        //    long seekDay;
        //    if (usageSinceDay <= DateTime.Now.Day)
        //    {
        //        seekDay = long.Parse(
        //            DateTime.Now.Year.ToString() +
        //            DateTime.Now.Month.ToString("00") +
        //            (DateTime.Now.Day - (DateTime.Now.Day - usageSinceDay)).ToString("00"));
        //    }
        //    else
        //    {
        //        seekDay = long.Parse(
        //            DateTime.Now.Year.ToString() +
        //            DateTime.Now.AddMonths(-1).Month.ToString("00") +
        //            usageSinceDay.ToString("00"));
        //    }

        //    //read data
        //    _reloadLogfile();

        //    foreach (var ni in _localMachine.Interfaces.Values)
        //    {
        //        //early continue
        //        if (ni.InterfaceId != interfaceId) { continue; }

        //        //get data
        //        var dayReadings = _getDailyUsage(ni);
        //        foreach (var dr in dayReadings.Values)
        //        {
        //            //early continue
        //            if (dr.Day < seekDay) { continue; }

        //            result += dr.BytesReceived + dr.BytesSent;
        //        }

        //        break;
        //    }

        //    return result;
        //}

        //public Machine LocalMachine { get { return _localMachine; } }
        #endregion

        #region methods
        //public void ReadTrafficData(string fileName)
        //{
        //    _fileName = fileName;
        //    _reloadLogfile();
        //}
        #endregion
    }



    #region event args
    public class FileErrorEventArgs
    {
        public enum EventType
        {
            FileNotFound,
            Timeout,
            IOException,
            Exception
        }

        public EventType Type { get; set; }
        public int IOExceptionCount { get; set; } = 0;
        public int ExceptionCount { get; set; } = 0;
        public string LastMessage { get; set; } = string.Empty;
    }
    #endregion
}
