using System;
using System.IO;
using System.Net.NetworkInformation;
using System.Threading;
using vobsoft.net;

class Program
{
    static void Main(string[] args)
    {
        var t = new NetworkTrafficLogger();
        t.StartLogging();

        while (true)
        {
            _writeStats();

            ConsoleKeyInfo cki = Console.ReadKey();
            if (cki.Key == ConsoleKey.X) { break; }

            Thread.Sleep(1000);
        }

        //Console.ReadKey();
    }

    static void _writeStats()
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