using Microsoft.ServiceFabric.Services.Runtime;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ncsa.Services.StatelessComputeService
{
    class KeepAliveService : StatelessService
    {
        public KeepAliveService(StatelessServiceContext context)
            : base(context)
        {
            ServiceEventSource.Current.ServiceMessage(this, "*** ctor for {0}", context.ServiceName);

        }
    
    }
}
