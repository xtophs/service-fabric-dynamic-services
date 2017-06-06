using Microsoft.AspNetCore.Hosting;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Runtime;
using Ncsa.Services.Schedulders.Service;
using Ncsa.Services.Scheduler.Shared;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Fabric;
using System.Fabric.Description;
using System.Fabric.Query;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ncsa.Services.StaticScheduler
{
    public class Program
    {
        // Entry point for the application.
        public static void Main(string[] args)
        {
            ServiceRuntime.RegisterServiceAsync("StaticSchedulerType", context => new WebHostingService(context, "ServiceEndpoint")).GetAwaiter().GetResult();

            Thread.Sleep(Timeout.Infinite);
        }

        /// <summary>
        /// A specialized stateless service for hosting ASP.NET Core web apps.
        /// </summary>
        internal sealed class WebHostingService : Microsoft.ServiceFabric.Services.Runtime.StatelessService, ICommunicationListener
        {
            private readonly string _endpointName;

            private IWebHost _webHost;

            public WebHostingService(StatelessServiceContext serviceContext, string endpointName)
                : base(serviceContext)
            {
                _endpointName = endpointName;
            }

            #region StatelessService

            protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
            {
                return new[] { new ServiceInstanceListener(_ => this) };
            }

            #endregion StatelessService

            #region ICommunicationListener

            void ICommunicationListener.Abort()
            {
                _webHost?.Dispose();
            }

            Task ICommunicationListener.CloseAsync(CancellationToken cancellationToken)
            {
                _webHost?.Dispose();

                return Task.FromResult(true);
            }

            Task<string> ICommunicationListener.OpenAsync(CancellationToken cancellationToken)
            {
                var endpoint = FabricRuntime.GetActivationContext().GetEndpoint(_endpointName);

                string serverUrl = $"{endpoint.Protocol}://{FabricRuntime.GetNodeContext().IPAddressOrFQDN}:{endpoint.Port}";

                _webHost = new WebHostBuilder().UseKestrel()
                                                .UseWebRoot("scheduler")
                                               .UseContentRoot(Directory.GetCurrentDirectory())
                                               .UseStartup<Startup>()
                                               .UseUrls(serverUrl)
                                               .Build();

                _webHost.Start();

                return Task.FromResult(serverUrl);
            }

            #endregion ICommunicationListener



            /// <summary>
            /// This is the main entry point for your service's partition replica. 
            /// RunAsync executes when the primary replica for this partition has write status.
            /// </summary>
            /// <param name="cancelServicePartitionReplica">Canceled when Service Fabric terminates this partition's replica.</param>
            protected override async Task RunAsync(CancellationToken cancelServicePartitionReplica)
            {

                // This partition's replica continues processing until the replica is terminated.
                while (!cancelServicePartitionReplica.IsCancellationRequested)
                {
                    // No logic in here
                    // functionalaity is only invoked via remote WebAPI interface 

                    // Pause for 1 second before continue processing.
                    await Task.Delay(TimeSpan.FromSeconds(1), cancelServicePartitionReplica);
                }
            }
        }

    }
}
