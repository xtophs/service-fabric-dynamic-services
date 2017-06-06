using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Ncsa.Services.Scheduler.Shared;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using LogViewService.Shared;

namespace PartitionedComputeService
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class PartitionedComputeService : StatefulService, IJobWorker
    {
        public PartitionedComputeService(StatefulServiceContext context)
            : base(context)
        {
            ServiceEventSource.Current.ServiceMessage(this, "*** ctor for {0}, Parition {1}", context.ServiceName, context.PartitionId);
            
        }

        protected override Task OnChangeRoleAsync(ReplicaRole newRole, CancellationToken cancellationToken)
        {
            ServiceEventSource.Current.ServiceMessage(this, "*** OnChangeRole for {0}, Parition {1}. new role is: {2}", this.Context.ServiceName, this.Context.PartitionId, newRole.ToString());
            // TODO: record role change

            return base.OnChangeRoleAsync(newRole, cancellationToken);
        }
        /// <summary>
        /// Optional override to create listeners (e.g., HTTP, Service Remoting, WCF, etc.) for this service replica to handle client or user requests.
        /// </summary>
        /// <remarks>
        /// For more information on service communication, see http://aka.ms/servicefabricservicecommunication
        /// </remarks>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new List<ServiceReplicaListener> {

                new ServiceReplicaListener(initParams => this.CreateServiceRemotingListener<PartitionedComputeService>(initParams))
            };
        }

        /// <summary>
        /// This is the main entry point for your service replica.
        /// This method executes when this replica of your service becomes primary and has write status.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service replica.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            ServiceEventSource.Current.ServiceMessage(this, "Activating Service: {0}, on Node {1}, Parition: {2} ", this.Context.ServiceName, this.Context.NodeContext.NodeName, this.Context.PartitionId);

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }

        static int load = 0;

        public Task<Job> Execute(Job j)
        {
            int loadfactor = Convert.ToInt32(j.InitData)/1000; // 1sec (1000ms) -> 1 load factor
            if (loadfactor < 0) loadfactor = 1;

            for (int i = 0; i < loadfactor; i++)
            {
                Interlocked.Increment(ref load);
            }

            var m = new LoadMetric("TCU", load);

            this.Partition.ReportLoad(new LoadMetric[] { m });

            var msg = String.Format("executing job {0} Node: {1}, Partition {2} ", j.Id, this.Context.NodeContext.NodeName, this.Partition.PartitionInfo.Id);
            LogClient.Log(msg);

            var t = new JobExecutor().Execute(j);
            msg = string.Format("DONE executing job {0} Node: {1}, Partition {2} ", j.Id, this.Context.NodeContext.NodeName, this.Partition.PartitionInfo.Id);
            LogClient.Log(msg);

            for (int i = 0; i < loadfactor; i++)
            {
                Interlocked.Decrement(ref load);
            }

            m = new LoadMetric("TCU", load);
            this.Partition.ReportLoad(new LoadMetric[] { m });

            return t; 
        }
    }
}
