using System.IO;
using NetworkTrafficWatcherWPFLiteDB.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using vobsoft.net;
using vobsoft.net.LiteDBLogger.model;
using Vobsoft.Libraries.WPF;
using System.Threading;

namespace NetworkTrafficWatcherWPFLiteDB.ViewModels
{
    internal class MainWindowViewModel : ViewModelBase
    {
        #region declarations
        string _logFile;
        FileSystemWatcher _fsw;
        NetworkTrafficWatcherModelLiteDB _ntw;
        #endregion

        #region constructor
        public MainWindowViewModel()
        {
            //get logfile
            _logFile = Helpers.GetLogFilename();

            _ntw = NetworkTrafficWatcherModelLiteDB.Instance;
            _ntw.Filename = _logFile;

            foreach (var ai in _ntw.LocalInterfaces)
            {
                AvailableInterfaces.Add(ai);
            }

            OnPropertyChanged("AvailableInterfaces");

            _fsw = new FileSystemWatcher(Path.GetDirectoryName(_logFile));
            _fsw.Changed += Fsw_Changed;
            _fsw.EnableRaisingEvents = true;
        }
        #endregion

        #region private functions
        private void Fsw_Changed(object sender, FileSystemEventArgs e)
        {
            if (e.Name == Path.GetFileName(_logFile))
            {
                Thread.Sleep(1000);
                _showSlectedData();
            }
        }

        private void _showSlectedData()
        {
            //early exit
            if (SelectedInterface == null) { return; }

                //ntw.ReadTrafficData(_logFile);

                //_getDailyUsagesData();
                _getTodaysUsageData();
                _getUsageDataSince();
        }

        private void _getDailyUsagesData()
        {
                //_ntw.ReadTrafficData(_logFile);
                //Results = ntw.GetDailyUsagesOfInterface(SelectedInterface.InterfaceId);
        }

        private void _getTodaysUsageData()
        {
            long todaysUsage = _ntw.GetTodaysUsageOfInterface(SelectedInterface.Id);
            //Results = todaysUsage.ToString("N0");

            TodaysGiB = ((double)todaysUsage / 1000000000).ToString("#,##0.00");// + " GiB";
            TodaysMiB = ((double)todaysUsage / 1000000).ToString("#,##0.00");// + " MiB";
        }

        private void _getUsageDataSince()
        {
            //long UsageSince = _ntw.GetUsageOfInterfaceSince(SelectedInterface.InterfaceId, 5);
            //Results = UsageSince.ToString("N0");

            //GiBSince = ((double)UsageSince / 1000000000).ToString("#,##0.00");// + " GiB";
            //MiBSince = ((double)UsageSince / 1000000).ToString("#,##0.00");// + " MiB";
        }
        #endregion

        #region properties
        private ObservableCollection<LocalNetworkInterface> _availableInterfaceItems = new ObservableCollection<LocalNetworkInterface>();
        public ObservableCollection<LocalNetworkInterface> AvailableInterfaces
        {
            get { return _availableInterfaceItems; }
            set
            {
                _availableInterfaceItems = value;
                OnPropertyChanged("AvailableInterfaces");
            }
        }

        private LocalNetworkInterface _selectedInterface;
        public LocalNetworkInterface SelectedInterface
        {
            get { return _selectedInterface; }
            set
            {
                _selectedInterface = value;
                _showSlectedData();
            }
        }

        private string _results;
        public string Results
        {
            get { return _results; }
            set
            {
                _results = value;
                OnPropertyChanged("Results");
            }
        }

        private string _todaysGiB;
        public string TodaysGiB
        {
            get { return _todaysGiB; }
            set
            {
                _todaysGiB = value;
                OnPropertyChanged("TodaysGiB");
            }
        }

        private string _todaysMiB;
        public string TodaysMiB
        {
            get { return _todaysMiB; }
            set
            {
                _todaysMiB = value;
                OnPropertyChanged("TodaysMiB");
            }
        }

        private string _GiBSince;
        public string GiBSince
        {
            get { return _GiBSince; }
            set
            {
                _GiBSince = value;
                OnPropertyChanged("GiBSince");
            }
        }

        private string _MiBSince;
        public string MiBSince
        {
            get { return _MiBSince; }
            set
            {
                _MiBSince = value;
                OnPropertyChanged("MiBSince");
            }
        }
        #endregion

        #region methods

        #endregion
    }
}
