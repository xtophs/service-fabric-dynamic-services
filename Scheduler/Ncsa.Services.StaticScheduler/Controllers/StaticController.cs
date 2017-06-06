
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Ncsa.Services.Schedulders.Service;
using Ncsa.Services.Scheduler.Shared;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Fabric;
using System.Fabric.Description;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulerAPI.Controllers
{

    [Route("api/[controller]")]
    public class StaticController
    {
        private readonly IStaticScheduler scheduler;

        public StaticController()
        {
   
        }

            // in an ideal REST world this would be a PUT but hey
        [HttpGet]
        [Route("{time}")]
        public async Task<IActionResult> GetNewJob(int time)
        {
            try
            {
                var job = NewJob(time);
                return new ObjectResult(job);
            }
            catch( Exception ex)
            {
                return new ObjectResult(ex);
            }
  
        }

        public Task<ConcurrentDictionary<string, int>> GetInstances()
        {
            return Task.FromResult(clusterNodes);
        }

        Random r = new Random();

        public Task<Job[]> GetStatus()
        {
            List<Job> j = new List<Job>();
            lock (jobsLock)
            {
                //using (var tx = this.StateManager.CreateTransaction())
                //{
                //    using (var e = jobs.CreateEnumerableAsync(tx).Result.GetAsyncEnumerator())
                //    {
                //        while (e.MoveNextAsync(new CancellationToken()).Result)
                //        {
                //            j.Add(e.Current.Value);

                //        }
                //    }
                //}
            }

            return Task.FromResult(j.ToArray());
        }

        
        Uri serviceURI = new Uri("fabric:/Compute/ComputeServiceService");
        ConcurrentDictionary<string, int> clusterNodes = new ConcurrentDictionary<string, int>();
        ConcurrentDictionary<string, Job> jobs = new ConcurrentDictionary<string, Job>();
        //IReliableDictionary<string, Job> jobs;
        Object jobsLock = new object();


        public async Task<Job> NewJob(int time)
        {

            var j = new Job()
            {
                NodeName = "",
                Id = Guid.NewGuid().ToString(),
                Status = JobStatus.Waiting,
                InitData = time.ToString()
            };

            lock (jobs)
            {
                jobs.AddOrUpdate(j.Id, j, (o, n) => j);
            }

            var key = Fnv1aHashCode.Get64bitHashCode(j.Id);

            IJobWorker proxy = ServiceProxy.Create<IJobWorker>(new Uri("fabric:/Compute2/PartitionedComputeService"),
                new ServicePartitionKey(key));
            proxy.Execute(j).ConfigureAwait(false);

            return j;
        }

        public Task<Job> DiagStateful(int replicas)
        {
            var j = new Job()
            {
                NodeName = "", // TODO
                Id = Guid.NewGuid().ToString(),
                Status = JobStatus.Waiting
            };

            var ser = JsonSerializer.Create();

            var sw = new StringWriter();
            ser.Serialize(sw, j);

            StatefulServiceDescription desc = new StatefulServiceDescription();
            try
            {

                FabricClient client = new FabricClient();


                desc.ApplicationName = new Uri("fabric:/Compute2");
                desc.DefaultMoveCost = MoveCost.High;
                desc.ServiceName = new Uri(string.Format("fabric:/Compute2/EmptyStatefulInstance{0}", System.DateTime.Now.Ticks));
                desc.ServiceTypeName = "EmptyServiceType";
                desc.PartitionSchemeDescription = new SingletonPartitionSchemeDescription();
                desc.TargetReplicaSetSize = replicas;
                desc.MinReplicaSetSize = 1;
                desc.HasPersistedState = false;
                desc.InitializationData = Encoding.UTF8.GetBytes(sw.ToString());
                var metric = new StatefulServiceLoadMetricDescription();
                metric.PrimaryDefaultLoad = 1;
                metric.SecondaryDefaultLoad = 1;
                metric.Name = "calc";
                metric.Weight = ServiceLoadMetricWeight.Medium;
                desc.Metrics.Add(metric);


                client.ServiceManager.CreateServiceAsync(desc).Wait();
                //ServiceEventSource.Current.ServiceMessage(this, string.Format("created on svc {0}", desc.ServiceName));
            }
            catch (Exception ex)
            {

                // CreateServiceAsync throws when there's no capacity for StatefulServices
                // Handle appropriately
                //ServiceEventSource.Current.ServiceMessage(this, string.Format("ERROR creating svc {0}, {1}", desc.ServiceName, ex.ToString()));

                Debug.WriteLine(ex.ToString());
                return Task.FromResult(RecordErrorStatus(j));

            }
            return Task.FromResult<Job>(j);

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

    }
}
