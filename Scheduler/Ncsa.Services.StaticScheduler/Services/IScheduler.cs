using Microsoft.ServiceFabric.Services.Remoting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ncsa.Services.Scheduler.Shared
{
    public interface IStaticScheduler : IService
    {
        Task<Job> NewJob(int time);
        Task<Job> DiagStateful(int replicas);
        Task<string> Complete(string Id);
        Task<Job[]> GetStatus();
        Task<ConcurrentDictionary<string, int>> GetInstances();
    }

    public interface IDynamicScheduler : IService
    {
        Task<Job> NewJobService(int time, bool isStateful );
        Task<Job> DiagStateful(int replicas);
        Task<Job[]> GetStatus();
    }
}