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
            _dictInterfacesDBIDs = new Dictionary<string, long>();
            _initDatabasefile();
            _initTimer();
            _initDatabase();
        }

        ~NetworkTrafficLogger()
        {
            //its already disposed???
            //_sqLite.Dispose();
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
            if (!NetworkInterface.GetIsNetworkAvailable() || _lngMachineDBID == 0) { return; }

            //prerequisites
            string strSQL = "SELECT * FROM tblInterfaces WHERE MachineId = @prmMachineId AND InterfaceId = @prmInterfaceId;";
            var prmMachineId = new SqlParameter() { Name = "@prmMachineId", Type = DbType.Int64, Value = _lngMachineDBID };
            var prmInterfaceId = new SqlParameter() { Name = "@prmInterfaceId", Type = DbType.String };
            _dictInterfacesDBIDs.Clear();

            //go through interface collection
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface ni in interfaces)
            {
                prmInterfaceId.Value = ni.Id;

                //check if Interface already exists
                if (!_sqLite.HasRows(strSQL, new List<SqlParameter>() { prmMachineId, prmInterfaceId }))
                {
                    //create it
                    Dictionary<string, object> dic = new Dictionary<string, object>();
                    dic.Add("MachineId", _lngMachineDBID);
                    dic.Add("Name", ni.Name);
                    dic.Add("Description", ni.Description);
                    dic.Add("InterfaceId", ni.Id);
                    dic.Add("Type", ni.NetworkInterfaceType.ToString());
                    dic.Add("Status", ni.OperationalStatus.ToString());
                    dic.Add("Speed", ni.Speed);

                    var affectedRows = _sqLite.WriteDataToTable("tblInterfaces", dic);
                    if (affectedRows < 1)
                    {
                        throw new Exception("Something went wrong when writing data to table.");
                    }
                }

                //remember ids of current interfaces in db
                strSQL = "SELECT * FROM tblInterfaces WHERE MachineId = @prmMachineId;";
                DataTable dt = _sqLite.GetDataTable(strSQL, new List<SqlParameter>() { prmMachineId });
                if (dt.Rows.Count > 0)
                {
                    DataRow dr = dt.Rows[0];
                    if (long.TryParse(dr["id"].ToString(), out long retval))
                    {
                        if (!_dictInterfacesDBIDs.ContainsKey(ni.Id))
                        {
                            _dictInterfacesDBIDs.Add(ni.Id, retval);
                        }
                    }
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

                //get current time
                var dtNow = DateTime.Now;
                long tsNow = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();

                //prepare
                var strSQL = "INSERT INTO tblReadings (InterfaceId, LogTime, LogTimeOld, BytesReceived, BytesSent) VALUES " +
                    "(@prmInterfaceId, @prmLogTime, @prmLogTimeOld, @prmBytesReceived, @prmBytesSent)";
                var prmInterfaceId = new SqlParameter() { Name = "@prmInterfaceId", Type = DbType.Int64 };
                var prmLogTime = new SqlParameter() { Name = "@prmLogTime", Type = DbType.Int64 };
                var prmLogTimeOld = new SqlParameter() { Name = "@prmLogTimeOld", Type = DbType.Int64 };
                var prmLogTimeString = new SqlParameter() { Name = "@LogTimeString", Type = DbType.String };
                var prmLogTimeString2 = new SqlParameter() { Name = "@LogTimeString2", Type = DbType.String };
                var prmTst = new SqlParameter() { Name = "@prmTst", Type = DbType.Int64 };
                var prmBytesReceived = new SqlParameter() { Name = "@prmBytesReceived", Type = DbType.Int64 };
                var prmBytesSent = new SqlParameter() { Name = "@prmBytesSent", Type = DbType.Int64 };

                //build log line
                NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
                foreach (NetworkInterface ni in interfaces)
                {
                    //early continue
                    if (!_dictInterfacesDBIDs.ContainsKey(ni.Id)) { continue; }

                    long.TryParse(dtNow.ToString("yyyyMMddhhmmss"), out long lngDateNow);
                    //create it via table schema (recommended)
                    Dictionary<string, object> dic = new Dictionary<string, object>();
                    dic.Add("InterfaceId", _dictInterfacesDBIDs[ni.Id]);
                    dic.Add("LogTime", tsNow);
                    dic.Add("LogTimeOld", tsNow);
                    dic.Add("LogTimeString", tsNow.ToString());
                    dic.Add("LogTimeString2", dtNow.ToString("yyyyMMddhhmmss"));
                    dic.Add("Tst", lngDateNow);
                    dic.Add("BytesReceived", ni.GetIPv4Statistics().BytesReceived);
                    dic.Add("BytesSent", ni.GetIPv4Statistics().BytesSent);

                    var affectedRows = _sqLite.WriteDataToTable("tblReadings", dic);
                    if (affectedRows < 1)
                    {
                        throw new Exception("Something went wrong when writing data to table.");
                    }



                    //create it via prepared SQL statement (2nd place recommended)
                    //prmInterfaceId.Value = _dictInterfacesDBIDs[ni.Id];
                    //prmLogTime.Value = tsNow;
                    //prmLogTimeOld.Value = tsNow;
                    //prmBytesReceived.Value = ni.GetIPv4Statistics().BytesReceived;
                    //prmBytesSent.Value = ni.GetIPv4Statistics().BytesSent;
                    //var lstParams = new List<SqlParameter>() {
                    //    prmInterfaceId,
                    //    prmLogTime,
                    //    prmLogTimeOld,
                    //    prmBytesReceived,
                    //    prmBytesSent
                    //};
                    //if (_sqLite.ExecutePreparedStatement(strSQL, lstParams) != 1)
                    //{
                    //    throw new Exception("Something went wrong when writing data to table.");
                    //}


                    //create it manually generated SQL statement (not recommended)
                    //strSQL = strSQL.Replace("@prmInterfaceId", _dictInterfacesDBIDs[ni.Id].ToString());
                    //strSQL = strSQL.Replace("@prmLogTimeOld", tsNow.ToString());
                    //strSQL = strSQL.Replace("@prmLogTime", tsNow.ToString());
                    //strSQL = strSQL.Replace("@prmBytesReceived", ni.GetIPv4Statistics().BytesReceived.ToString());
                    //strSQL = strSQL.Replace("@prmBytesSent", ni.GetIPv4Statistics().BytesSent.ToString());
                    //if (_sqLite.ExecuteSqlWithRowsAffected(strSQL) != 1)
                    //{
                    //    throw new Exception("Something went wrong when writing data to table.");
                    //}
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
