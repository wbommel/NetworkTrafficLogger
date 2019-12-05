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
                    var dbmachines = db.GetCollection("machines");
                    dbmachines.Insert<vobsoft.net.LiteDBLogger.model.Machine>(new vobsoft.net.LiteDBLogger.model.Machine() {
                        MachineName=Environment.MachineName
                    });
                }
            }
        }
    }
}
