using LogViewService.Shared.SFLogger;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogViewService.Shared
{
    public class LogClient
    {
        public static void Log(string text)
        {
            var logClient = ServiceProxy.Create<ILogService>(new Uri("fabric:/LogKeeper/LogViewService"));
            logClient.AddLog(DateTime.Now, text);
        }
    }
}
