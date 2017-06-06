using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Ncsa.Services.Scheduler.Shared;

// For more information on enabling Web API for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace Ncsa.Services.Schedulders.Controllers
{
    [Route("api/[controller]")]
    public class DynamicStatefulController : Microsoft.AspNetCore.Mvc.ControllerBase 
    {
        private readonly IDynamicScheduler scheduler;

        public DynamicStatefulController(IDynamicScheduler schedulerService)
        {
            scheduler = schedulerService;

        }

        // GET: api/values
        [HttpGet]
        [Route("{time}")]
        public async Task<IActionResult> Get(int time)
        {
            try
            {
                var job = scheduler.NewJobService(time, true);
                return new ObjectResult(job);
            }
            catch (Exception ex)
            {
                return new ObjectResult(ex);
            }
        }
    }
}
