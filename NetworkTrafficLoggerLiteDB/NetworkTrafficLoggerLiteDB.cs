﻿using LiteDB;
using System;
using System.IO;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Timers;
using vobsoft.net.LiteDBLogger.model;
using vobsoft.net.LiteDBLogger.Properties;

namespace vobsoft.net.LiteDBLogger
{
    public class NetworkTrafficLoggerLiteDB
    {
        #region declarations
        private double _timerMin = 200; // .2 seconds
        private double _timerMax = 60 * 60 * 24 * 1000; //1 day
        private Timer _tmrLoggingInterval;

        private bool _fLoggingActive = false;

        //private LiteCollection<Machine> _allMachines;
        //private Machine _localMachine;

        private const string PATTERN_ASSEMBLY_LOCATION = "$(AssemblyLocation)";
        #endregion

        #region constructor
        public NetworkTrafficLoggerLiteDB()
        {
            _initTimer();
            _initLogfile();
        }

        ~NetworkTrafficLoggerLiteDB()
        {
            //its already disposed???
            //_sqLite.Dispose();
        }
        #endregion

        #region private functions
        private void _initLogfile()
        {
            //get settings
            Logfile = Settings.Default.Logfile;

            if (Logfile.Contains(PATTERN_ASSEMBLY_LOCATION))
            {
                Logfile = Logfile.Replace(PATTERN_ASSEMBLY_LOCATION, Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            }

            //throw a lot of exceptions. :)
            if (string.IsNullOrEmpty(Logfile))
            {
                throw new ArgumentNullException("Logfile", "Argument is empty. No Logfile given.");
            }

            if (!Directory.Exists(Path.GetDirectoryName(Logfile)))
            {
                throw new DirectoryNotFoundException("_initLogfile. Directory not found: " + Path.GetDirectoryName(Logfile));
            }

            _checkTrafficDataEntries();
        }

        private void _initTimer()
        {
            //get interval setting
            double dblInterval = Settings.Default.Interval;
            if (dblInterval < _timerMin) { dblInterval = _timerMin; }
            if (dblInterval > _timerMax) { dblInterval = _timerMax; }

            //initialize timer
            _tmrLoggingInterval = new Timer(dblInterval);

            //create event listener
            _tmrLoggingInterval.Elapsed += _tmrLoggingInterval_Elapsed;
        }

        private void _checkTrafficDataEntries()
        {
            using (var db = new LiteDatabase(Logfile))
            {
                //fetch machine or create it
                var localMachine = _fetchLocalMachine(db);

                //early exit when no networking interfaces are present
                if (!NetworkInterface.GetIsNetworkAvailable()) { return; }

                //load interfaces
                var dbInterfaces = db.GetCollection<LocalNetworkInterface>(Constants.COLLECTION_INTERFACES);

                //go through interface collection
                NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
                foreach (NetworkInterface ni in interfaces)
                {
                    //early continue if interface exists and has equal properties
                    if (dbInterfaces.Exists(x => x.InterfaceGUID == ni.Id)) { continue; }


                    //insert network interface to db
                    dbInterfaces.Insert(new LocalNetworkInterface()
                    {
                        MachineId = localMachine.Id,
                        Name = ni.Name,
                        Description = ni.Description,
                        InterfaceGUID = ni.Id,
                        Type = ni.NetworkInterfaceType.ToString(),
                        Status = ni.OperationalStatus.ToString(),
                        Speed = ni.Speed
                    });
                }
            }
        }

        private Machine _fetchLocalMachine(LiteDatabase db)
        {
            //get TrafficData of db
            var allMachines = db.GetCollection<Machine>(Constants.COLLECTION_MACHINES);
            Machine localMachine;

            //get machine
            if (!allMachines.Exists(x => x.MachineName == Environment.MachineName))
            {
                localMachine = new Machine() { MachineName = Environment.MachineName };
                allMachines.Insert(localMachine);
            }
            else
            {
                localMachine = allMachines.FindOne(x => x.MachineName == Environment.MachineName);
            }

            return localMachine;
        }

        private void _tmrLoggingInterval_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!_fLoggingActive) { _doLogging(); }
        }

        private void _doLogging()
        {
            //set active
            _fLoggingActive = true;

            try
            {
                using (var db = new LiteDatabase(Logfile))
                {
                    //early exit when no networking interfaces are present
                    if (!NetworkInterface.GetIsNetworkAvailable()) { return; }

                    //get data
                    var localMachine = _fetchLocalMachine(db);
                    var dbInterfaces = db.GetCollection<LocalNetworkInterface>(Constants.COLLECTION_INTERFACES);
                    var dbReadings = db.GetCollection<Reading>(Constants.COLLECTION_READINGS);

                    //eventually delete broken entries here (Error: Cannot insert duplicate key in unique index '_id'. The duplicate value is '81511'.)
                    //this error can be solved by deleting all entries behind the mentioned (i.e. 81511 - readings.count (i.e. 81515))
                    //THis would work:
                    //dbReadings.DeleteMany(Query.GT("_id", 81511));

                    //get current time
                    var dtNow = DateTime.Now;
                    long tsNow = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();

                    //build log line
                    NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
                    foreach (NetworkInterface ni in interfaces)
                    {
                        var lni = dbInterfaces.FindOne(x => x.InterfaceGUID == ni.Id);

                        //early continue
                        if (lni == null) { continue; }

                        long.TryParse(dtNow.ToString("yyyyMMddHHmmss"), out long lngDateNow);

                        dbReadings.Insert(new Reading()
                        {
                            InterfaceId = lni.Id,
                            LogTime = lngDateNow,
                            BytesReceived = ni.GetIPv4Statistics().BytesReceived,
                            BytesSent = ni.GetIPv4Statistics().BytesSent
                        });
                    }
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }
            finally
            {
                //set inactive
                _fLoggingActive = false;
            }
        }
        #endregion

        #region properties
        public string Logfile { get; private set; }

        public bool InstantWrite { get; set; }
        #endregion

        #region methods
        public void StartLogging()
        {
            _doLogging();//initial logging
            _tmrLoggingInterval.Start();
        }

        public void StopLogging()
        {
            _tmrLoggingInterval.Stop();
        }
        #endregion
    }
}
