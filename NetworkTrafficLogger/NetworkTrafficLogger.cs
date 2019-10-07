using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net.NetworkInformation;
using System.Timers;
using vobsoft.net.Properties;
using Vobsoft.Csharp.Database;

namespace vobsoft.net
{
    public class NetworkTrafficLogger
    {
        #region declarations
        private double _timerMin = 200; // .2 seconds
        private double _timerMax = 60 * 60 * 24 * 1000; //1 day
        private Timer _tmrLoggingInterval;

        private double _keepDbOpenValue = 60000; //below this, keep db connection open
        private SqLiteHandler _sqLite;

        private bool _fLoggingActive = false;

        private long _lngMachineDBID;
        private Dictionary<string, long> _dictInterfacesDBIDs;
        #endregion

        #region constructor
        public NetworkTrafficLogger()
        {
            _initDatabasefile();
            _initTimer();
            _initDatabase();
        }

        ~NetworkTrafficLogger()
        {
            _sqLite.Dispose();
        }
        #endregion

        #region private functions
        private void _initDatabasefile()
        {
            //get Logfile from session
            Logfile = Settings.Default.Logfile;

            //throw a lot of exceptions. :)
            if (string.IsNullOrEmpty(Logfile))
            {
                throw new ArgumentNullException("Database file", "Argument is empty. No Logfile given.");
            }

            if (!Directory.Exists(Path.GetDirectoryName(Logfile)))
            {
                throw new DirectoryNotFoundException("Database file directory not found: " + Path.GetDirectoryName(Logfile));
            }

            if (!File.Exists(Logfile))
            {
                throw new FileNotFoundException("Database file not found.", Logfile);
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

        private void _initDatabase()
        {
            try
            {
                //create db handler
                if (_tmrLoggingInterval.Interval < _keepDbOpenValue)
                {
                    _sqLite = new SqLiteHandler(Logfile, ConnectionBehaviour.AllwaysOpen);
                }
                else
                {
                    _sqLite = new SqLiteHandler(Logfile, ConnectionBehaviour.AutomaticOpenAndClose);
                }

                //check if MachineName, interfaces and such are already present in db an get their id's and all
                //if not, create them
                _createMachine();
                _createInterfaces();

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void _createMachine()
        {
            //prerequisites
            string strSQL = "SELECT * FROM tblMachines WHERE MachineName = @prmMachineName;";
            var prmMachineName = new SqlParameter() { Name = "@prmMachineName", Type = System.Data.DbType.String, Value = Environment.MachineName };

            //check if MachineName already exists
            if (!_sqLite.HasRows(strSQL, new List<SqlParameter>() { prmMachineName }))
            {
                //create it
                Dictionary<string, object> dic = new Dictionary<string, object>();
                dic.Add("MachineName", Environment.MachineName);
                var affectedRows = _sqLite.WriteDataToTable("tblMachines", dic);
                if (affectedRows < 1)
                {
                    throw new Exception("Something went wrong when writing data to table.");
                }
            }

            //remember machine id in db
            DataTable dt = _sqLite.GetDataTable(strSQL, new List<SqlParameter>() { prmMachineName });
            if (dt.Rows.Count > 0)
            {
                DataRow dr = dt.Rows[0];
                if (long.TryParse(dr["id"].ToString(), out long retval))
                {
                    _lngMachineDBID = retval;
                }
            }
        }

        private void _createInterfaces()
        {
            //early exit when no networking interfaces are present
            if (!NetworkInterface.GetIsNetworkAvailable()) { return; }

            //prerequisites
            string strSQL = "SELECT * FROM tblInterfaces WHERE InterfaceId = @prmInterfaceId;";
            var prmMachineName = new SqlParameter() { Name = "@prmInterfaceId", Type = System.Data.DbType.String };

            //go through interface collection
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface ni in interfaces)
            {
                xxxxxx
            }


            //check if MachineName already exists
            if (!_sqLite.HasRows(strSQL, new List<SqlParameter>() { prmMachineName }))
            {
                //create it
                Dictionary<string, object> dic = new Dictionary<string, object>();
                dic.Add("MachineName", Environment.MachineName);
                var affectedRows = _sqLite.WriteDataToTable("tblMachines", dic);
                if (affectedRows < 1)
                {
                    throw new Exception("Something went wrong when writing data to table.");
                }
            }

            //remember machine id in db
            DataTable dt = _sqLite.GetDataTable(strSQL, new List<SqlParameter>() { prmMachineName });
            if (dt.Rows.Count > 0)
            {
                DataRow dr = dt.Rows[0];
                if (long.TryParse(dr["id"].ToString(), out long retval))
                {
                    _lngMachineDBID = retval;
                }
            }
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

                //build log line
                NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
                foreach (NetworkInterface ni in interfaces)
                {

                }

                using (StreamWriter sw = File.AppendText("log.txt"))
                {

                }
            }
            catch (Exception ex)
            {

                throw;
            }
            finally
            {
                //set inactive
                _fLoggingActive = false;
            }
        }
        #endregion

        #region properties
        public string Logfile { get; set; }
        #endregion

        #region methods
        public void StartLogging()
        {
            _tmrLoggingInterval.Start();
        }

        public void StopLogging()
        {
            _tmrLoggingInterval.Stop();
        }
        #endregion
    }
}
