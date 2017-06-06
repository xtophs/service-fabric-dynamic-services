using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.ServiceFabric.Data;
using Ncsa.Services.Scheduler.Shared;
using System.IO;
using Newtonsoft.Json;
using System.Diagnostics;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using LogViewService.Shared;

namespace Ncsa.Services.StatefulComputeService
{
    internal sealed class StatefulComputeService : StatefulService, IJobWorker
    {
        public StatefulComputeService(StatefulServiceContext context) : base(context)
        {
            //ServiceEventSource.Current.ServiceMessage(this, "*** ctor for {0}, Parition {1}", context.ServiceName, context.PartitionId);

        }

        protected override Task OnChangeRoleAsync(ReplicaRole newRole, CancellationToken cancellationToken)
        {
            ServiceEventSource.Current.ServiceMessage(this, "*** OnChangeRole for {0}, Parition {1}. new role is: {2}", this.Context.ServiceName, this.Context.PartitionId, newRole.ToString());
            // TODO: record role change

            return base.OnChangeRoleAsync(newRole, cancellationToken);
        }

        public Task InitializeStateSerializers()
        {
            return Task.Factory.StartNew(() => { });
        }

        Uri appName = new Uri("fabric:/Compute2");

        Random r = new Random();

        /// <summary>
        /// Optional override to create listeners (like tcp, http) for this service instance.
        /// </summary>
        /// <returns>The collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new List<ServiceReplicaListener> {

                new ServiceReplicaListener(initParams => 
                this.CreateServiceRemotingListener<StatefulComputeService>(initParams))
            };
        }


        /// <summary>
        /// This is the main entry point for your service instance.
        /// </summary>
        /// <param name="cancelServiceInstance">Canceled when Service Fabric terminates this instance.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            Job j = null;
            using (var sr = new StringReader(System.Text.Encoding.UTF8.GetString(this.Context.InitializationData)))
            {
                using (var jr = new JsonTextReader(sr))
                {
                    j = JsonSerializer.Create().Deserialize<Job>(jr);
                }
            }
            if (j == null) throw new Exception("Bad init data");
            var msg = string.Format("Running Job {0} on Node {1}", j.Id, this.Context.NodeContext.NodeName);
            LogClient.Log(msg);
            ServiceEventSource.Current.ServiceMessage(this, msg);

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
