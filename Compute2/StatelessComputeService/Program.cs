using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Services.Runtime;
using System;
using System.Diagnostics;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;

namespace Ncsa.Services.StatelessComputeService
{
    internal static class Program
    {
        public static Task InitializeStateSerializers()
        {
            return Task.Factory.StartNew(() => { });

        }
        /// <summary>
        /// This is the entry point of the service host process.
        /// </summary>
        private static void Main()
        {
            try
            {
                //ServiceRuntime.RegisterServiceAsync("StatefulSvc",
                //    ctx => new StatefulSvc(ctx, new ReliableStateManagerConfiguration(onInitializeStateSerializersEvent: Program.InitializeStateSerializers))).GetAwaiter().GetResult();

                ServiceRuntime.RegisterServiceAsync("StatelessComputeServiceType",
                    context => new StatelessComputeService(context)).GetAwaiter().GetResult();
                //ServiceRuntime.RegisterServiceAsync("KeepAliveServiceType",
                //    context => new KeepAliveService(context)).GetAwaiter().GetResult();
                //ServiceRuntime.RegisterServiceAsync("EmptyServiceType",
                //    context => new EmptyStateful(context)).GetAwaiter().GetResult();
                ServiceEventSource.Current.ServiceTypeRegistered(Process.GetCurrentProcess().Id, typeof(StatelessComputeService).Name);

                // Prevents this host process from terminating so services keep running.

                Thread.Sleep(Timeout.Infinite);
            }
            catch (Exception e)
            {
                ServiceEventSource.Current.ServiceHostInitializationFailed(e.ToString());
                throw;
            }
        }
    }
}
