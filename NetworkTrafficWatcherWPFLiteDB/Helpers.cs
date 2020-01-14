using System.Reflection;

namespace NetworkTrafficWatcherWPFLiteDB.ViewModels
{
    internal class Helpers
    {
        public static string GetLogFilename()
        {
            //get logfile
#if DEBUG
            return System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\..\..\..\NetworkAdapterTest\bin\Debug\NetworkTraffic.db";
#else
            return Settings.Default.Logfile;
#endif
        }
    }
}
