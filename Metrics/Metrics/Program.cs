using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Metrics
{
    class Program
    {
        static void Main(string[] args)
        {

            Console.WriteLine("Service Fabric Metric Checker");
            try
            {
                DoStuff();
            }
            catch( Exception ex)
            {
                Console.WriteLine("Something bad happened");
                Console.WriteLine(ex.ToString());
            }
        }

        static void DoStuff()
        {
            var client = new FabricClient( "localhost:19000");
            Console.WriteLine("Getting Ready to check");

            while (true)
            {
                var load = client.QueryManager.GetClusterLoadInformationAsync().Result;
                var nodes = client.QueryManager.GetNodeListAsync().Result;

                var services = client.QueryManager.GetServiceListAsync(new Uri("fabric:/Compute2")).Result;
                var frogs = services.Where(s => s.ServiceName.OriginalString.IndexOf("ComputeInstance") >= 0);

                if (load.LoadMetricInformationList.Count > 0)
                {

                    //foreach (var lmi in load.LoadMetricInformationList.Where(l => l.Name == "TCU"))
                    foreach (var lmi in load.LoadMetricInformationList)
                    {
                        Console.WriteLine(String.Format("Metric: {0}, Cluster Capacity: {1}, Current: {2}, Violation {3}, MaxNode: {4}, MaxValue {5}", lmi.Name, lmi.ClusterCapacity, lmi.ClusterLoad, lmi.IsClusterCapacityViolation, lmi.MaxNodeLoadNodeId, lmi.MaxNodeLoadValue));
                    }
                }
                else
                {
                    Console.WriteLine("No TCU Load");
                }

                //foreach ( var node in nodes )
                //{
                //    var nodeLoadInfo = client.QueryManager.GetNodeLoadInformationAsync(node.NodeName).Result;
                //    var nodeTcuLoad = nodeLoadInfo.NodeLoadMetricInformationList.FirstOrDefault(n => n.Name == "TCU");
                //    if (nodeTcuLoad != null)
                //    {
                //        Console.WriteLine(string.Format("Node {0}, TCUs {1}, Capacitiy {2}, Violation {3}", node.NodeName, nodeTcuLoad.NodeLoad, nodeTcuLoad.NodeCapacity, nodeTcuLoad.IsCapacityViolation));
                //    }
                //}

                //if (frogs != null)
                //{
                //    Console.WriteLine(String.Format("Frog Count {0}", frogs.Count()));
                //}
                //else
                //{
                //    Console.Write("No Frog");
                //}
                Thread.Sleep(20000);
            }

        }
    }
}
