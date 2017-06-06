using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MetricCheckerConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            while( true )
            {
                Console.WriteLine(DoStuff().Result);
                Thread.Sleep(1000);
            }
        }

         static async Task<string> DoStuff()
        {
            var client = new FabricClient();
            var sb = new StringBuilder();
            var load = await client.QueryManager.GetClusterLoadInformationAsync();
            var services = await client.QueryManager.GetServiceListAsync(new Uri("fabric:/Compute2"));
            var frogs = services.Where(s => s.ServiceName.OriginalString.IndexOf("ComputeInstance") >= 0);

            if (load.LoadMetricInformationList.Count > 0)
            {
                foreach (var lmi in load.LoadMetricInformationList)
                {
                    sb.Append(String.Format("Metric: {0}, Capacity: {1}, Current: {2}\n", lmi.Name, lmi.ClusterCapacity, lmi.ClusterLoad));
                }
            }
            else
            {
                sb.Append("No Load\n");
            }

            if (frogs != null)
            {
                sb.Append(String.Format("Frog Count {0}", frogs.Count()));
            }
            else
            {
                sb.Append("No Frog");
            }

            return sb.ToString();


        }
    }
}
