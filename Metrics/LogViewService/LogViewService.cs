using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.ServiceFabric.Services.Remoting;
using System.Collections.Concurrent;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using LogViewService.Shared.SFLogger;
using System.Diagnostics;

namespace LogViewService
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class LogViewService : StatelessService, ILogService
    {
        ConcurrentDictionary<DateTime, LogEntry> logEntries = new ConcurrentDictionary<DateTime, LogEntry>();

        public LogViewService(StatelessServiceContext context)
            : base(context)
        {
            var e = new LogEntry() { timestamp = DateTime.Now, text = "firstone" };
            logEntries.AddOrUpdate(DateTime.Now, e, (o, n) => e);
        }

        public async Task AddLog(DateTime logtimestamp, string logtext)
        {
            var e = new LogEntry() { timestamp = logtimestamp, text = logtext };
            Debug.WriteLine(String.Format("adding: {0}", logtext));
            logEntries.AddOrUpdate(DateTime.Now, e, (o, n) => e );
        }

        public async Task<IEnumerable<LogEntry>> GetLogEntries()
        {
            return logEntries.Values;
        }

        public async Task Clear()
        {
            logEntries.Clear();
        }

        /// <summary>
        /// Optional override to create listeners (e.g., TCP, HTTP) for this service replica to handle client or user requests.
        /// </summary>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return new List<ServiceInstanceListener> {

                new ServiceInstanceListener(initParams => this.CreateServiceRemotingListener<LogViewService>(initParams))
            };
        }

        /// <summary>
        /// This is the main entry point for your service instance.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service instance.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following sample code with your own logic 
            //       or remove this RunAsync override if it's not needed in your service.


            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
    }
}
