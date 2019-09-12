using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace TrafficLogger
{
    internal class TrafficWriter
    {
        #region declarations
        string _fileName = Path.Combine(Path.GetTempPath(), "NetworkTrafficLogger", "TrafficLog.csv");
        string _strFormat = "{0};{1};{2};{3};{4};{5};{6};{7};{8};{9}";
        #endregion

        #region constructor
        public TrafficWriter()
        {
            if (!File.Exists(_fileName))
            {
                _writeToFile(string.Format(_strFormat, "Date", "Time", "Name", "Description", "Id", "NetworkInterfaceType", "OperationalStatus", "Speed", "recieved bytes", "sent bytes"));
            }
        }
        #endregion

        #region private functions
        private void _writeToFile(string line)
        {
            using (TextWriter tw = new StreamWriter(_fileName))
            {
                tw.WriteLine(line);
            }
        }
        #endregion

        #region properties
        #endregion

        #region methods
        public void WriteLogEntry()
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
                return;

            NetworkInterface[] interfaces
                = NetworkInterface.GetAllNetworkInterfaces();

            foreach (NetworkInterface ni in interfaces)
            {
                var dtNow = DateTime.Now;
                _writeToFile(string.Format(_strFormat, dtNow.ToShortDateString(), dtNow.ToLongTimeString(), ni.Name, ni.Description, ni.Id, ni.NetworkInterfaceType.ToString(), ni.OperationalStatus.ToString(), ni.Speed, ni.GetIPv4Statistics().BytesReceived, ni.GetIPv4Statistics().BytesSent));
            }
        }
        #endregion
    }
}
