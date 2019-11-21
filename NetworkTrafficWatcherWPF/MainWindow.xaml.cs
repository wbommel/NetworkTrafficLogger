using NetworkTrafficWatcherWPF.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using vobsoft.net;

namespace NetworkTrafficWatcherWPF
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string _logFile;

        public MainWindow()
        {
            InitializeComponent();

            //get logfile
            _logFile = Settings.Default.Logfile;

#if DEBUG
            _logFile = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\..\..\..\NetworkAdapterTest\bin\Debug\NetworkTraffic.json";
#endif
            using(var ntw=new NetworkTrafficWatcher())
            {
                ntw.ReadTrafficData(_logFile);
                lblResult.Content = ntw.TestOutput;
            }
        }



    }
}
