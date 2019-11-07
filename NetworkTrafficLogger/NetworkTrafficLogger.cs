using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.NetworkInformation;
using System.Timers;
using vobsoft.net.model;
using vobsoft.net.Properties;

namespace vobsoft.net
{
    public class NetworkTrafficLogger
    {
        #region declarations
        private double _timerMin = 200; // .2 seconds
        private double _timerMax = 60 * 60 * 24 * 1000; //1 day
        private Timer _tmrLoggingInterval;

        private bool _fLoggingActive = false;

        private Machine _localMachine;
        #endregion

        #region constructor
        public NetworkTrafficLogger()
        {
            _initTimer();
            _initLogfile();
        }

        ~NetworkTrafficLogger()
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
            InstantWrite = Settings.Default.InstantWrite;

            //throw a lot of exceptions. :)
            if (string.IsNullOrEmpty(Logfile))
            {
                throw new ArgumentNullException("Logfile", "Argument is empty. No Logfile given.");
            }

            if (!Directory.Exists(Path.GetDirectoryName(Logfile)))
            {
                throw new DirectoryNotFoundException("_initLogfile directory not found: " + Path.GetDirectoryName(Logfile));
            }

            if (!File.Exists(Logfile))
            {
                _createLogFileJSON();
            }
            else
            {
                _loadLogFile();
            }
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

        private void _createLogFileJSON()
        {

            _localMachine = new Machine() { MachineName = Environment.MachineName };

            //early exit when no networking interfaces are present
            if (!NetworkInterface.GetIsNetworkAvailable()) { return; }

            //go through interface collection
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface ni in interfaces)
            {
                //early continue if interface exists and has equal properties
                if (_localMachine.Interfaces.ContainsKey(ni.Id) &&
                    _localMachine.Interfaces[ni.Id].Description == ni.Description &&
                    _localMachine.Interfaces[ni.Id].InterfaceId == ni.Id &&
                    _localMachine.Interfaces[ni.Id].Type == ni.NetworkInterfaceType.ToString() &&
                    _localMachine.Interfaces[ni.Id].Status == ni.OperationalStatus.ToString() &&
                    _localMachine.Interfaces[ni.Id].Speed == ni.Speed) { continue; }

                //add network interface to Logfile
                _localMachine.Interfaces.Add(ni.Id, new LocalNetworkInterface()
                {
                    Name = ni.Name,
                    Description = ni.Description,
                    InterfaceId = ni.Id,
                    Type = ni.NetworkInterfaceType.ToString(),
                    Status = ni.OperationalStatus.ToString(),
                    Speed = ni.Speed
                });
            }

            _saveLogfile();
        }

        private void _saveLogfile()
        {
            File.WriteAllText(Logfile, JsonConvert.SerializeObject(_localMachine));
        }

        private void _loadLogFile()
        {
            _localMachine = JsonConvert.DeserializeObject<Machine>(File.ReadAllText(Logfile));
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
                //early exit when no networking interfaces are present
                if (!NetworkInterface.GetIsNetworkAvailable()) { return; }

                //get current time
                var dtNow = DateTime.Now;
                long tsNow = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();

                //build log line
                NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
                foreach (NetworkInterface ni in interfaces)
                {
                    //early continue
                    if (!_localMachine.Interfaces.ContainsKey(ni.Id)) { continue; }

                    long.TryParse(dtNow.ToString("yyyyMMddHHmmss"), out long lngDateNow);

                    _localMachine.Interfaces[ni.Id].Readings.Add(dtNow.ToString("yyyyMMddHHmmss"), new Reading()
                    {
                        LogTime = dtNow.ToString("yyyyMMddHHmmss"),
                        BytesReceived = ni.GetIPv4Statistics().BytesReceived,
                        BytesSent = ni.GetIPv4Statistics().BytesSent
                    });
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }
            finally
            {
                //instant write logfile
                if (InstantWrite) { _saveLogfile(); }

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
            _saveLogfile();
        }
        #endregion
    }
}
