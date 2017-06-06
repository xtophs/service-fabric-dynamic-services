using LogViewService.Shared;
using Microsoft.ServiceFabric.AspNetCore.Hosting;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
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
using System.Linq;
using System.Net;
//using System.Net.Http;
//using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ncsa.Services.Scheduler
{


    /// <summary>
    /// The FabricRuntime creates an instance of this class for each service type instance.
    /// </summary>
    internal sealed class GatedDynamicSchedulerService : Microsoft.ServiceFabric.Services.Runtime.StatelessService, IDynamicScheduler
    {
        private readonly AspNetCoreCommunicationContext _communicationContext;
        private readonly SemaphoreSlim _semaphore;

        public GatedDynamicSchedulerService(StatelessServiceContext serviceContext,
                AspNetCoreCommunicationContext communicationContext)
            : base(serviceContext)
        {
            _communicationContext = communicationContext;
            _semaphore = new SemaphoreSlim(1, 1);
            
        }
        //protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        //{
        //    return new[] { new ServiceReplicaListener(_ => _communicationContext.CreateCommunicationListener(this)) };
        //}

        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return new[] { new ServiceInstanceListener(_ => _communicationContext.CreateCommunicationListener(this)) };
        }

        //protected override Task OnChangeRoleAsync(ReplicaRole newRole, CancellationToken cancellationToken)
        //{
            
        //    return base.OnChangeRoleAsync(newRole, cancellationToken);  
        //}
        Random r = new Random();

        //ServicePartitionList partList = null;
        Uri serviceURI = new Uri("fabric:/Compute/ComputeServiceService");
        ConcurrentDictionary<string, int> clusterNodes = new ConcurrentDictionary<string, int>();
        ConcurrentDictionary<string, Job> jobs = new ConcurrentDictionary<string, Job>();
        //IReliableDictionary<string, Job> jobs;
        Object jobsLock = new object();

        public Task<Job> DiagStateful(int replicas)
        {
            // TODO - I think I had this only for debugging
            return Task.FromResult<Job>(new Job());
        }

        
        Task<Job[]> GetStatus()
        {
            // TODO
            var ja = new Job[1];
            ja[0] = new Job();
            return Task.FromResult<Job[]>(ja);
        }


        public Task<Job> NewJobService( int time, bool isStateful  )
        {

            var j = new Job()
            {
                NodeName = "", // TODO
                Id = Guid.NewGuid().ToString(),
                Status = JobStatus.Waiting,
                InitData = time.ToString()         
            };

            var client = new FabricClient();
            ServiceDescription desc = null;

            if( isStateful )
            {
                desc = GetStatefulDescription(j);
            }
            else
            {
                desc = GetStatelessDescription(j);
            }

            try
            {
                client.ServiceManager.CreateServiceAsync(desc);
                ServiceEventSource.Current.ServiceMessage(this, string.Format("CREATED Service Instance {0} for Job {1}", desc.ServiceName, j.Id));
                LogClient.Log(string.Format("CREATED Service Instance {0} for Job {1}", desc.ServiceName, j.Id));
            }
            catch (Exception ex)
            {

                // CreateServiceAsync throws when there's no capacity for StatefulServices
                // Handle appropriately

                Debug.WriteLine(ex.ToString());
                var msg = string.Format("*** Exception creating service {0}\n{1}", desc.ServiceName, ex.ToString());
                LogClient.Log(msg);
                ServiceEventSource.Current.ServiceMessage(this, msg);
                return Task.FromResult(RecordErrorStatus(j));

            }

            // optional monitoring code
            // here for demonstration purposes
            // this should probably running somewhere else
            var count = 0;
            var isBad = true;
            do
            {
                //ServiceEventSource.Current.ServiceMessage(this, string.Format("checking on svc {0}", desc.ServiceName));
                try
                {

                    var partition = client.QueryManager.GetPartitionListAsync(desc.ServiceName).Result;
                    if (partition != null && partition.FirstOrDefault() != null)
                    {
                        var p = partition.FirstOrDefault();
                        if (p.PartitionStatus == ServicePartitionStatus.Ready
                            && p.HealthState == System.Fabric.Health.HealthState.Ok)
                        {
                            // everything copacetic ... service came up
                            isBad = false;
                            break;
                        }
                        else if (p.PartitionStatus == ServicePartitionStatus.NotReady
                            && p.HealthState == System.Fabric.Health.HealthState.Error)
                        {
                            // this usually means the cluster was full
                            // no need to try again
                            ServiceEventSource.Current.ServiceMessage(this, "PROBLEM Job {0} did not come up on Node {1}", j.Id, this.Context.NodeContext.NodeName);
                            break;
                        }

                        else if (p.HealthState == System.Fabric.Health.HealthState.Error)
                        {
                            // theoretically, this could be a transient error ... 
                            // for now, we bail. Probably want to check if error was transient.
                            ServiceEventSource.Current.ServiceMessage(this, "PROBLEM Job {0} did not come up on Node {1}", j.Id, this.Context.NodeContext.NodeName);
                            break;
                        }

                        Thread.Sleep(500);
                    }
                    else
                    {
                        // if the partition is not there, it probably means work already completed and 
                        // the service instance was deleted. 
                        // Want more sophisticated logic in Azure
                        isBad = false;
                        break;
                        //Debugger.Break(); // in case we want to see that we get here. Don't turn on in Azure!!
                    }
                }
                catch(Exception ex)
                {
                    Debug.Write(ex.ToString());
                }

            } while (count++ < 12);

            if (isBad == true)
            {
                //ServiceEventSource.Current.ServiceMessage(this, string.Format("Error starting svc {0}", desc.ServiceName));

                Debug.WriteLine("Error starting: " + desc.ServiceName);
                j = RecordErrorStatus(j);
            }
           // if (isBad == false) ServiceEventSource.Current.ServiceMessage(this, string.Format("Success starting svc {0}", desc.ServiceName));

            return Task.FromResult<Job>(j);

        }

        internal StatelessServiceDescription GetStatelessDescription( Job j )
        {
            var ser = JsonSerializer.Create();

            var sw = new StringWriter();
            ser.Serialize(sw, j);

            var desc = new StatelessServiceDescription()
            {
                ApplicationName = new Uri("fabric:/Compute2"),
                DefaultMoveCost = MoveCost.High,
                ServiceName = new Uri(string.Format("fabric:/Compute2/ComputeInstance{0}", System.DateTime.Now.Ticks)),
                ServiceTypeName = "StatelessComputeServiceType",
                PartitionSchemeDescription = new SingletonPartitionSchemeDescription(),
                InstanceCount = 1,
                InitializationData = Encoding.UTF8.GetBytes(sw.ToString())
            };

            var metric = new StatelessServiceLoadMetricDescription()
            {
                DefaultLoad = 1,
                Name = "TCU",
                Weight = ServiceLoadMetricWeight.Medium

            };
            desc.Metrics.Add(metric);

            return desc;
        }

        internal StatefulServiceDescription GetStatefulDescription(Job j)
        {
            var ser = JsonSerializer.Create();

            var sw = new StringWriter();
            ser.Serialize(sw, j);

            var desc = new StatefulServiceDescription()
            {
                ApplicationName = new Uri("fabric:/Compute2"),
                DefaultMoveCost = MoveCost.High,
                ServiceName = new Uri(string.Format("fabric:/Compute2/ComputeInstance{0}", System.DateTime.Now.Ticks)),
                ServiceTypeName = "StatefulComputeServiceType",
                HasPersistedState = true,
                PartitionSchemeDescription = new SingletonPartitionSchemeDescription(),
                TargetReplicaSetSize= 3,
                MinReplicaSetSize = 2,
                InitializationData = Encoding.UTF8.GetBytes(sw.ToString())
            };

            var metric = new StatefulServiceLoadMetricDescription()
            {
                PrimaryDefaultLoad = 1,
                SecondaryDefaultLoad = 0,
                Name = "TCU",
                Weight = ServiceLoadMetricWeight.Medium

            };
            desc.Metrics.Add(metric);

            return desc;
        }
        Job RecordErrorStatus(Job j)
        {
            j.Status = JobStatus.Error;
            j.Finished = System.DateTime.Now;

            return j;
        }
        public Task<string> Complete(string id)
        {
            if (id == null) id = "";
            //ServiceEventSource.Current.ServiceMessage(this, string.Format("Completing {0} ", id));

            lock (jobsLock)
            {
                try
                {
                    Job j = null;
                    //using (var tx = this.StateManager.CreateTransaction())
                    //using (var tx = jobs.TryGetValue(  CreateTransaction())
                    {
                        jobs.TryGetValue(id, out j);
                        //j = jobs.TryGetValueAsync(tx, id).Result.Value;
                        var newJ = j;
                        newJ.Status = JobStatus.Finished;
                        //jobs.TryUpdateAsync(tx, id, newJ, newJ);
                        jobs.TryUpdate(id, newJ, newJ);
                        //tx.CommitAsync().Wait();
                    }
                    // jobs[id].Status = JobStatus.Finished;
                    clusterNodes[j.NodeName.ToLower()] = 0;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("problem cleaning up " + ex.ToString());
                }
            }
            return Task.FromResult("ok");
        }


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

        Task<Job[]> IDynamicScheduler.GetStatus()
        {
            throw new NotImplementedException();
        }
    }
}
