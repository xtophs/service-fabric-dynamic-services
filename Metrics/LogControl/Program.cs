using LogViewService.Shared.SFLogger;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogControl
{
    class Program
    {
        static void Main(string[] args)
        {
            var logClient = ServiceProxy.Create<ILogService>(new Uri("fabric:/LogKeeper/LogViewService"));
            logClient.AddLog(DateTime.Now, "console logon");
            var entries = logClient.GetLogEntries().Result;
            foreach( var e in entries)
            {
                Console.WriteLine("{0}: {1}", e.timestamp, e.text);
            }
        }
    }
}
