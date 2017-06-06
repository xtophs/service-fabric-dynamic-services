using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Fabric;
using System.Text;

namespace MetricAPI.Controllers
{
    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        // GET api/values
        [HttpGet]
        public async Task<string> Get()
        {
            var client = new FabricClient();
            var sb = new StringBuilder();
            var load = await client.QueryManager.GetClusterLoadInformationAsync();
            var services = await client.QueryManager.GetServiceListAsync(new Uri("fabric:/Compute2"));
            var frogs = services.Where(s => s.ServiceName.OriginalString.IndexOf("ComputeInstance") >= 0);

            if (load.LoadMetricInformationList.Count > 0)
            {
                foreach (var lmi in load.LoadMetricInformationList)
                {
                    sb.Append(String.Format("Metric: {0}, Capacity: {1}, Current: {2}\n", lmi.Name, lmi.ClusterCapacity, lmi.ClusterLoad));
                }
            }
            else
            {
                sb.Append("No Load\n");
            }

            if (frogs != null)
            {
                sb.Append(String.Format("Frog Count {0}", frogs.Count()));
            }
            else
            {
                sb.Append("No Frog");
            }
            
            return sb.ToString();
            //return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody]string value)
        {
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
