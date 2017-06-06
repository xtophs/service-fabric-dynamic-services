using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ncsa.Services.Scheduler.Shared
{
    public class JobExecutor : IJobWorker
    {
        public Task<Job> Execute(Job j)
        {

            int millis = 10000;
            if( ! String.IsNullOrEmpty( j.InitData ) )
            {
                var n = Convert.ToInt32(j.InitData);
                Debug.WriteLine("Sleep duration: {0}", n);
                millis = n;
            }
            Thread.Sleep(millis);

            j.Finished = System.DateTime.Now;
            j.Status = JobStatus.Finished;
            j.NodeName = System.Net.Dns.GetHostName();

            return Task.FromResult(j);
        }
    }
}
