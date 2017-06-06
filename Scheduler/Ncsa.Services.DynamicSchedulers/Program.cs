using Microsoft.AspNetCore.Hosting;
using Microsoft.ServiceFabric.AspNetCore.Hosting;
using Microsoft.ServiceFabric.Services.Runtime;
using Ncsa.Services.Scheduler;
using System.IO;

namespace Counter
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var communicationContext = CreateAspNetCoreCommunicationContext();

            ServiceRuntime.RegisterServiceAsync("DynamicSchedulerType", serviceContext => new GatedDynamicSchedulerService(serviceContext, communicationContext)).GetAwaiter().GetResult();

            communicationContext.WebHost.Run();
        }

        private static AspNetCoreCommunicationContext CreateAspNetCoreCommunicationContext()
        {
            var webHost = new WebHostBuilder().UseKestrel()
                                              .UseContentRoot(Directory.GetCurrentDirectory())                                              
                                              .UseStartup<Startup>()
                                              .UseWebRoot("scheduler")
                                              .UseServiceFabricEndpoint("SchedulerTypeEndpoint")
                                              .Build();


            return new AspNetCoreCommunicationContext(webHost);
        }
    }
}
