using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using System.Diagnostics;
using LogViewService.Shared.SFLogger;


namespace LogView.Controllers
{

    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            var logClient = ServiceProxy.Create<ILogService>(new Uri("fabric:/LogKeeper/LogViewService"));
            var entries = logClient.GetLogEntries().Result;
            foreach( var e in entries)
            {
                Debug.WriteLine(e.text);
            }

            return View();
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
