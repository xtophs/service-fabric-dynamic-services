
using Microsoft.AspNetCore.Mvc;
using Ncsa.Services.Scheduler.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SchedulerAPI.Controllers
{

    [Route("api/[controller]")]
    public class DynamicController
    {
        private readonly IDynamicScheduler scheduler;

        public DynamicController(IDynamicScheduler schedulerService)
        {
            scheduler = schedulerService;
        }

        // in an ideal REST world this would be a PUT but hey
        [HttpGet]
        [Route("{time}")]
        public async Task<IActionResult> GetNewJob(int time)
        {
            try
            {
                var job = scheduler.NewJobService(time, false);
                return new ObjectResult(job);
            }
            catch( Exception ex)
            {
                return new ObjectResult(ex);
            }
        }
    }
}
