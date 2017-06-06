using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Fabric;
using System.Fabric.Description;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Notifications;
using Ncsa.Services.Scheduler.Shared;
using LogViewService.Shared;

namespace Ncsa.Services.StatelessComputeService
{


    /// <summary>
    /// The FabricRuntime creates an instance of this class for each service type instance. 
    /// </summary>
    internal sealed class StatelessComputeService : StatelessService
    {

        Uri appName = new Uri("fabric:/Compute2");

        public StatelessComputeService(StatelessServiceContext context)
            : base(context)
        {
            //ServiceEventSource.Current.ServiceMessage(this, "*** ctor for {0}", context.ServiceName);

        }
        Random r = new Random();

        /// <summary>
        /// Optional override to create listeners (like tcp, http) for this service instance.
        /// </summary>
        /// <returns>The collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            // TODO: If your service needs to handle user requests, return a list of ServiceReplicaListeners here.
            return new ServiceInstanceListener[0];
        }



        /// <summary>
        /// This is the main entry point for your service instance.
        /// </summary>
        /// <param name="cancelServiceInstance">Canceled when Service Fabric terminates this instance.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            var msg = string.Format("Activating Service: {0}, Parition: {1} on Node {2}", this.Context.ServiceName, this.Context.PartitionId, this.Context.NodeContext.NodeName);
            //LogClient.Log(msg);
            ServiceEventSource.Current.ServiceMessage(this, msg);

            //try
            //{
            //    this.Partition.ReportLoad(new List<LoadMetric> { new LoadMetric("calc", 0) });
            //}
            //catch (Exception ex )
            //{
            //    Debug.WriteLine(ex.ToString());
            //    throw;
            //}

            Job j = null;
            using (var sr = new StringReader(System.Text.Encoding.UTF8.GetString(this.Context.InitializationData)))
            {
                using (var jr = new JsonTextReader(sr))
                {
                    j = JsonSerializer.Create().Deserialize<Job>(jr);
                }
            }
            if (j == null) throw new Exception("Bad init data");
            msg = string.Format("Running Job {0} on Node {1}", j.Id, this.Context.NodeContext.NodeName);
            ServiceEventSource.Current.ServiceMessage(this, msg);
            LogClient.Log(msg);
            var r = await new JobExecutor().Execute(j);
            msg = string.Format("DONE Job {0} on Node {1}", j.Id, this.Context.NodeContext.NodeName);
            LogClient.Log(msg);
            ServiceEventSource.Current.ServiceMessage(this, msg);

            await CleanUp(cancellationToken);
        }

        public Task<Job> Execute(Job j)
        {
            throw new NotImplementedException();
        }

        private async Task CleanUp(CancellationToken cancellationToken)
        {
            var client = new FabricClient();

            //Don't await! -> Timeout errors
            client.ServiceManager.DeleteServiceAsync(this.Context.ServiceName);

            try
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                }
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.ServiceMessage(this, "Exception exiting {0}: Job {1}", this.Context.ServiceName, ex.ToString());
            }


        }
    }
}

