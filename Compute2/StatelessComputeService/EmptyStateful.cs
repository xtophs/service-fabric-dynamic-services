using Microsoft.ServiceFabric.Services.Runtime;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ncsa.Services.StatelessComputeService
{
    class EmptyStateful : StatefulService
    {
        Uri appName = new Uri("fabric:/Compute2");

        public EmptyStateful(StatefulServiceContext context)
            : base(context)
        {
            ServiceEventSource.Current.ServiceMessage(this, "*** ctor for {0}", context.ServiceName);

        }
    
    }
}
