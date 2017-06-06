using Microsoft.ServiceFabric.Services;
using Microsoft.ServiceFabric.Services.Remoting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ncsa.Services.Scheduler.Shared
{
    public enum JobStatus
    {
        Waiting,
        Running,
        Finished,
        Error
    }

    public class Job
    {

        public string NodeName
        {
            get; set;
        }
        public String Id
        {
            get; set;
        }
        public DateTime Started
        {
            get; set;
        }
        public DateTime Finished
        {
            get; set;
        }
        public JobStatus Status { get; set; }

        public string InitData { get; set; }
    }


    public interface IJobWorker : IService
    {
        Task<Job> Execute(Job j);
    }
}
