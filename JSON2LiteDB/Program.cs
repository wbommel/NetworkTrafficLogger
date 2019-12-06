using LiteDB;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using vobsoft.net.model;

namespace JSON2LiteDB
{
    class Program
    {
        static void Main(string[] args)
        {
            string jsonFilename = @"E:\temp2\NetworkTraficTest\NetworkTraffic.json";
            string liteDBFilename = @"E:\temp2\NetworkTraficTest\NetworkTraffic.db";

            JSONData2LiteDB(_loadJSONFile(jsonFilename), liteDBFilename);

            Console.WriteLine("any key to exit");
            Console.ReadKey();
        }



        private static TrafficData _loadJSONFile(string filename)
        {
            return JsonConvert.DeserializeObject<TrafficData>(File.ReadAllText(filename));
        }

        private static void JSONData2LiteDB(TrafficData trafficJSON, string liteDBFilename)
        {
            using (var db = new LiteDatabase(liteDBFilename))
            {
                foreach (var machineData in trafficJSON.Machines.Values)
                {
                    Console.WriteLine(Environment.NewLine);
                    string format = "Machine created - ID: {0,-5} Name: {1,-30}";

                    //first create Machine
                    var dbmachines = db.GetCollection<vobsoft.net.LiteDBLogger.model.Machine>("machines");

                    var newMachine = new vobsoft.net.LiteDBLogger.model.Machine()
                    {
                        MachineName = Environment.MachineName
                    };
                    dbmachines.Insert(newMachine);

                    Console.WriteLine(string.Format(format, newMachine.Id, newMachine.MachineName));

                    foreach (var lni in machineData.Interfaces.Values)
                    {
                        Console.WriteLine(Environment.NewLine);
                        format = "Interface created - ID: {0,-5} Name: {1,-24} Description: {2,-32}";

                        var dbInterfaces = db.GetCollection<vobsoft.net.LiteDBLogger.model.LocalNetworkInterface>("interfaces");

                        var newInterface = new vobsoft.net.LiteDBLogger.model.LocalNetworkInterface()
                        {
                            MachineId = newMachine.Id,
                            Name = lni.Name,
                            InterfaceGUID = lni.InterfaceId,
                            Description = lni.Description,
                            Speed = lni.Speed,
                            Status = lni.Status,
                            Type = lni.Type
                        };
                        dbInterfaces.Insert(newInterface);

                        Console.WriteLine(format, newInterface.Id, newInterface.Name, newInterface.Description);

                        foreach (var reading in lni.Readings.Values)
                        {
                            format = "Reading created - ID: {0,-5} Logtime: {1,-14} Sent+Recieved: {2,-14}";

                            var dbReadings = db.GetCollection<vobsoft.net.LiteDBLogger.model.Reading>("readings");

                            var newReading = new vobsoft.net.LiteDBLogger.model.Reading()
                            {
                                InterfaceId = newInterface.Id,
                                LogTime = reading.LogTime,
                                BytesReceived = reading.BytesReceived,
                                BytesSent = reading.BytesSent
                            };

                            Console.WriteLine(format, newReading.Id, newReading.LogTime, newReading.BytesSent + newReading.BytesReceived);
                        }
                    }
                }
            }
        }
    }
}
