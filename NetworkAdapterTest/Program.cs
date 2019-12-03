using System;
using System.IO;
using System.Net.NetworkInformation;
using System.Threading;
using vobsoft.net;
using vobsoft.net.LiteDBLogger;

class Program
{
    static void Main(string[] args)
    {
        var ntlJSON = new NetworkTrafficLogger();
        ntlJSON.StartLogging();

        var ntlLiteDB = new NetworkTrafficLoggerLiteDB();
        ntlLiteDB.StartLogging();

        Console.WriteLine("Logging network statistics... (press x to exit)");

        while (true)
        {
            //_writeInterfaceStats();

            if (Console.KeyAvailable)
            {
                ConsoleKeyInfo cki = Console.ReadKey();
                if (cki.Key == ConsoleKey.X) { break; }
            }

            Thread.Sleep(1000);
        }

        ntlLiteDB.StopLogging();
        ntlJSON.StopLogging();
        

        //Console.ReadKey();
    }

    static void _writeInterfaceStats()
    {
        var nl = Environment.NewLine;

        Console.SetCursorPosition(0, 0);

        //var fileName = Path.Combine(Path.GetTempPath(), "NetworkTrafficLogger", "TrafficLog.csv");
        //Console.WriteLine(fileName);

        if (!NetworkInterface.GetIsNetworkAvailable())
            return;

        NetworkInterface[] interfaces
            = NetworkInterface.GetAllNetworkInterfaces();

        var strFormat = "{0,-30}{1,30}{2,30}";
        Console.WriteLine(string.Format(strFormat, "name", "recieved bytes", "sent bytes"));
        Console.WriteLine("------------------------------------------------------------------------------------------");

        foreach (NetworkInterface ni in interfaces)
        {
            Console.WriteLine(string.Format(strFormat,
                ni.Name,
                ni.GetIPv4Statistics().BytesReceived,
                ni.GetIPv4Statistics().BytesSent));
            Console.WriteLine(" Descri: " + ni.Description);
            Console.WriteLine(" ID    : " + ni.Id);
            Console.WriteLine(" ifType: " + ni.NetworkInterfaceType.ToString());
            Console.WriteLine(" Status: " + ni.OperationalStatus.ToString());
            Console.WriteLine(" Speed : " + ni.Speed);
            Console.WriteLine(nl);
        }

        Console.WriteLine(nl);
        Console.WriteLine(long.MaxValue);
    }
}