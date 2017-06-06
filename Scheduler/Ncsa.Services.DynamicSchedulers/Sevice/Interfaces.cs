using Microsoft.ServiceFabric.Services.Remoting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogViewService.Shared
{
    namespace SFLogger
    {
        public class LogEntry
        {
            public DateTime timestamp;
            public string text;
        }

        public interface ILogService : IService
        {
            Task AddLog(DateTime logtimestamp, string text);
            Task<IEnumerable<LogEntry>> GetLogEntries();
            Task Clear();
        }

    }
}
